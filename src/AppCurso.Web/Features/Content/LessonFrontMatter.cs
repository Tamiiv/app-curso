namespace AppCurso.Web.Features.Content;

internal sealed class LessonFrontMatter
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public int EstimatedMinutes { get; init; }
    public string Summary { get; init; } = string.Empty;
    public List<LessonQuizFrontMatter> Quiz { get; init; } = [];
}

internal sealed class LessonQuizFrontMatter
{
    public string Question { get; init; } = string.Empty;
    public List<string> Options { get; init; } = [];
    public int Answer { get; init; }
    public string Explanation { get; init; } = string.Empty;
}
