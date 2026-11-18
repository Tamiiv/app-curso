using System.Text.RegularExpressions;

using AppCurso.Web.Features.Content;

namespace AppCurso.Tests.Features.Content;

public sealed partial class ContentCatalogTests
{
    private static readonly ContentCatalog Catalog = ContentLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "content"));

    [Fact]
    public void LoadsAllModulesInOrder()
    {
        Assert.Equal(13, Catalog.Modules.Count);
        Assert.Equal(Enumerable.Range(0, 13), Catalog.Modules.Select(module => module.Order));
    }

    [Fact]
    public void PublishedModulesExposeRenderedLessons()
    {
        var publishedModules = Catalog.Modules.Where(module => module.IsPublished).ToArray();

        Assert.NotEmpty(publishedModules);
        Assert.All(publishedModules, module =>
        {
            Assert.NotEmpty(module.Lessons);
            Assert.All(module.Lessons, lesson =>
            {
                Assert.False(string.IsNullOrWhiteSpace(lesson.Html));
                Assert.False(string.IsNullOrWhiteSpace(lesson.Summary));
                Assert.NotEmpty(lesson.Headings);
                Assert.True(lesson.HasQuiz, $"La lección {lesson.Url} no tiene quiz.");
            });
        });
    }

    [Fact]
    public void PlannedModulesExposeTopicsWithoutLessons()
    {
        var plannedModules = Catalog.Modules.Where(module => !module.IsPublished).ToArray();

        Assert.All(plannedModules, module =>
        {
            Assert.Empty(module.Lessons);
            Assert.NotEmpty(module.Topics);
        });
    }

    [Fact]
    public void EveryModuleIsEitherPublishedWithLessonsOrPlannedWithTopics()
    {
        Assert.All(Catalog.Modules, module =>
            Assert.True(
                module.IsPublished ? module.Lessons.Count > 0 : module.Topics.Count > 0,
                $"El módulo {module.Slug} no tiene el contenido esperado para su estado."));
    }

    [Fact]
    public void ComoUsarModuleHasTheThreeIntroductoryLessons()
    {
        var module = Catalog.GetModule("como-usar");

        Assert.NotNull(module);
        Assert.True(module.IsPublished);
        Assert.Equal(
            ["como-usar-la-plataforma", "progreso-marcadores", "como-practicar"],
            module.Lessons.Select(lesson => lesson.Slug));
    }

    [Fact]
    public void PublishedLessonsHaveUniqueUrls()
    {
        var urls = Catalog.AllLessons.Select(lesson => lesson.Url).ToArray();

        Assert.Equal(urls.Length, urls.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(
            Catalog.AllLessons,
            lesson => Assert.Equal($"/modulos/{lesson.ModuleSlug}/{lesson.Slug}", lesson.Url));
    }

    [Fact]
    public void SearchIgnoresAccentsAndFindsLessons()
    {
        var results = Catalog.Search("instalacion");

        Assert.Contains(results, result => result.Lesson.Slug == "instalar-dotnet");
    }

    [Fact]
    public void AdjacentLessonsFollowModuleOrder()
    {
        var (previous, next) = Catalog.GetAdjacentLessons("fundamentos", "que-es-dotnet");

        Assert.Null(previous);
        Assert.NotNull(next);
        Assert.Equal("instalar-dotnet", next.Slug);
    }

    [Fact]
    public void EveryPublishedLessonHasAtLeastThreeValidQuizQuestions()
    {
        Assert.All(Catalog.AllLessons, lesson =>
        {
            Assert.True(
                lesson.Quiz.Count >= 3,
                $"La lección {lesson.Url} tiene {lesson.Quiz.Count} preguntas de quiz.");

            Assert.All(lesson.Quiz, question =>
            {
                Assert.True(question.IsValid, $"Pregunta inválida en {lesson.Url}: {question.Question}");
                Assert.False(string.IsNullOrWhiteSpace(question.Explanation));
                Assert.Equal(question.Options.Count, question.Options.Distinct(StringComparer.Ordinal).Count());
            });
        });
    }

    [Fact]
    public void InternalLessonLinksPointToPublishedLessons()
    {
        var lessonUrls = Catalog.AllLessons
            .Select(lesson => lesson.Url)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var moduleUrls = Catalog.Modules
            .Select(module => $"/modulos/{module.Slug}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var brokenLinks = new List<string>();

        foreach (var lesson in Catalog.AllLessons)
        {
            foreach (Match match in InternalLinkPattern().Matches(lesson.Html))
            {
                var url = match.Groups["url"].Value.TrimEnd('/');

                if (url.Contains('#', StringComparison.Ordinal))
                {
                    url = url[..url.IndexOf('#', StringComparison.Ordinal)];
                }

                if (!lessonUrls.Contains(url) && !moduleUrls.Contains(url))
                {
                    brokenLinks.Add($"{lesson.Url} -> {url}");
                }
            }
        }

        Assert.Empty(brokenLinks);
    }

    [GeneratedRegex("href=\"(?<url>/modulos/[^\"]*)\"")]
    private static partial Regex InternalLinkPattern();
}
