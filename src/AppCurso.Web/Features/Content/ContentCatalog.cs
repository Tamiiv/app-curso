using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AppCurso.Web.Features.Content;

public sealed class ContentCatalog
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "al", "algo", "algunas", "algunos", "ante", "antes", "como", "con", "contra",
        "cual", "cuando", "de", "del", "desde", "donde", "durante", "e", "el", "ella",
        "ellas", "ellos", "en", "entre", "era", "es", "esa", "esas", "ese", "eso",
        "esos", "esta", "estas", "este", "esto", "estos", "fue", "ha", "hasta", "hay",
        "la", "las", "le", "les", "lo", "los", "mas", "me", "mi", "mis", "mucho",
        "muy", "nada", "ni", "no", "nos", "o", "otra", "otras", "otro", "otros",
        "para", "pero", "poco", "por", "porque", "que", "quien", "se", "sin",
        "sobre", "su", "sus", "tambien", "te", "tiene", "todo", "todos", "tu", "tus",
        "un", "una", "uno", "unos", "y", "ya", "yo"
    };

    private readonly IReadOnlyList<ContentModule> _modules;
    private readonly Dictionary<string, ContentModule> _modulesBySlug;
    private readonly Dictionary<string, ContentLesson> _lessonsByKey;
    private readonly IReadOnlyList<ContentLesson> _allLessons;

    public ContentCatalog(IReadOnlyList<ContentModule> modules)
    {
        _modules = modules;
        _modulesBySlug = modules.ToDictionary(module => module.Slug, StringComparer.OrdinalIgnoreCase);
        _allLessons = modules.SelectMany(module => module.Lessons).ToArray();
        _lessonsByKey = _allLessons.ToDictionary(
            lesson => BuildLessonKey(lesson.ModuleSlug, lesson.Slug),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ContentModule> Modules => _modules;
    public IReadOnlyList<ContentLesson> AllLessons => _allLessons;

    public ContentModule? GetModule(string slug) =>
        _modulesBySlug.GetValueOrDefault(slug);

    public ContentLesson? GetLesson(string moduleSlug, string lessonSlug) =>
        _lessonsByKey.GetValueOrDefault(BuildLessonKey(moduleSlug, lessonSlug));

    public IReadOnlyList<SearchResult> Search(string? query, int maxResults = 10)
    {
        var terms = Tokenize(query);
        if (terms.Count == 0)
        {
            return [];
        }

        var results = new List<SearchResult>();

        foreach (var lesson in _allLessons)
        {
            var title = Normalize(lesson.Title);
            var summary = Normalize(lesson.Summary);
            var headings = Normalize(string.Join(' ', lesson.Headings.Select(heading => heading.Text)));
            var body = Normalize(lesson.PlainText);
            var score = 0;

            foreach (var term in terms)
            {
                if (title.Contains(term, StringComparison.Ordinal))
                {
                    score += 10;
                }

                if (summary.Contains(term, StringComparison.Ordinal))
                {
                    score += 4;
                }

                if (headings.Contains(term, StringComparison.Ordinal))
                {
                    score += 2;
                }

                if (body.Contains(term, StringComparison.Ordinal))
                {
                    score += 1;
                }
            }

            if (score > 0)
            {
                results.Add(new SearchResult(lesson, BuildSnippet(lesson.PlainText), score));
            }
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Lesson.Title, StringComparer.Ordinal)
            .Take(maxResults)
            .ToArray();
    }

    public (ContentLesson? Previous, ContentLesson? Next) GetAdjacentLessons(string moduleSlug, string lessonSlug)
    {
        var module = GetModule(moduleSlug);
        if (module is null)
        {
            return (null, null);
        }

        var index = -1;
        for (var position = 0; position < module.Lessons.Count; position++)
        {
            if (string.Equals(module.Lessons[position].Slug, lessonSlug, StringComparison.OrdinalIgnoreCase))
            {
                index = position;
                break;
            }
        }

        if (index < 0)
        {
            return (null, null);
        }

        var previous = index > 0 ? module.Lessons[index - 1] : null;
        var next = index < module.Lessons.Count - 1 ? module.Lessons[index + 1] : null;
        return (previous, next);
    }

    private static string BuildLessonKey(string moduleSlug, string lessonSlug) =>
        $"{moduleSlug}/{lessonSlug}";

    private static string BuildSnippet(string text)
    {
        var compact = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 180 ? compact : $"{compact[..177]}...";
    }

    private static IReadOnlyList<string> Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return Regex.Split(Normalize(query), @"[^\p{L}\p{N}]+")
            .Where(term => term.Length > 1 && !StopWords.Contains(term))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
