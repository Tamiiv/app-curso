using System.Text;
using System.Text.Json;

using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AppCurso.Web.Features.Content;

public static class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers()
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static ContentCatalog Load(string contentRoot)
    {
        if (!Directory.Exists(contentRoot))
        {
            return new ContentCatalog([]);
        }

        var modules = new List<ContentModule>();

        foreach (var moduleDirectory in Directory.EnumerateDirectories(contentRoot))
        {
            var manifestPath = Path.Combine(moduleDirectory, "module.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = JsonSerializer.Deserialize<ModuleManifest>(
                File.ReadAllText(manifestPath),
                JsonOptions);

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Slug))
            {
                continue;
            }

            var status = ParseStatus(manifest.Status);
            var lessons = status == ModuleStatus.Published
                ? LoadLessons(moduleDirectory, manifest)
                : [];

            modules.Add(new ContentModule(
                manifest.Slug,
                manifest.Title,
                manifest.Order,
                manifest.Level,
                manifest.Summary,
                status,
                manifest.Topics,
                lessons));
        }

        return new ContentCatalog(modules
            .OrderBy(module => module.Order)
            .ThenBy(module => module.Title, StringComparer.Ordinal)
            .ToArray());
    }

    private static IReadOnlyList<ContentLesson> LoadLessons(string moduleDirectory, ModuleManifest manifest)
    {
        var lessons = new List<ContentLesson>();

        foreach (var lessonPath in Directory.EnumerateFiles(moduleDirectory, "*.md"))
        {
            var text = File.ReadAllText(lessonPath);
            var (frontMatter, body) = ParseFrontMatter(text);
            var fallbackSlug = Path.GetFileNameWithoutExtension(lessonPath);

            var document = Markdown.Parse(body, MarkdownPipeline);

            lessons.Add(new ContentLesson(
                string.IsNullOrWhiteSpace(frontMatter.Slug) ? fallbackSlug : frontMatter.Slug,
                string.IsNullOrWhiteSpace(frontMatter.Title) ? fallbackSlug : frontMatter.Title,
                frontMatter.Order,
                frontMatter.EstimatedMinutes,
                frontMatter.Summary,
                document.ToHtml(MarkdownPipeline),
                Markdown.ToPlainText(body, MarkdownPipeline),
                manifest.Slug,
                manifest.Title,
                ExtractHeadings(document),
                ExtractQuiz(frontMatter.Quiz)));
        }

        return lessons
            .OrderBy(lesson => lesson.Order)
            .ThenBy(lesson => lesson.Title, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<LessonHeading> ExtractHeadings(MarkdownDocument document)
    {
        var headings = new List<LessonHeading>();

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level is < 2 or > 3)
            {
                continue;
            }

            var text = GetHeadingText(heading);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            headings.Add(new LessonHeading(
                heading.GetAttributes().Id ?? string.Empty,
                text,
                heading.Level));
        }

        return headings;
    }

    private static IReadOnlyList<QuizQuestion> ExtractQuiz(List<LessonQuizFrontMatter>? questions)
    {
        if (questions is null || questions.Count == 0)
        {
            return [];
        }

        var quiz = new List<QuizQuestion>(questions.Count);

        foreach (var question in questions)
        {
            var options = question.Options
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select(option => option.Trim())
                .ToArray();

            if (string.IsNullOrWhiteSpace(question.Question) ||
                options.Length < 2 ||
                question.Answer < 0 ||
                question.Answer >= options.Length)
            {
                continue;
            }

            quiz.Add(new QuizQuestion(
                question.Question.Trim(),
                options,
                question.Answer,
                question.Explanation.Trim()));
        }

        return quiz;
    }

    private static string GetHeadingText(HeadingBlock heading)
    {
        var builder = new StringBuilder();
        AppendInlines(heading.Inline, builder);
        return builder.ToString().Trim();
    }

    private static void AppendInlines(ContainerInline? container, StringBuilder builder)
    {
        if (container is null)
        {
            return;
        }

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case ContainerInline nested:
                    AppendInlines(nested, builder);
                    break;
            }
        }
    }

    private static (LessonFrontMatter FrontMatter, string Body) ParseFrontMatter(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (new LessonFrontMatter(), normalized);
        }

        var end = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return (new LessonFrontMatter(), normalized);
        }

        var yaml = normalized[3..end].Trim();
        var body = normalized[(end + 4)..].TrimStart('\n');

        var frontMatter = YamlDeserializer.Deserialize<LessonFrontMatter>(yaml)
            ?? new LessonFrontMatter();

        return (frontMatter, body);
    }

    private static ModuleStatus ParseStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "published" => ModuleStatus.Published,
            "draft" => ModuleStatus.Draft,
            _ => ModuleStatus.Planned
        };
}
