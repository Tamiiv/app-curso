namespace AppCurso.Web.Features.Content;

public sealed record ContentModule(
    string Slug,
    string Title,
    int Order,
    string Level,
    string Summary,
    ModuleStatus Status,
    IReadOnlyList<string> Topics,
    IReadOnlyList<ContentLesson> Lessons)
{
    public bool IsPublished => Status == ModuleStatus.Published;
    public bool HasLessons => Lessons.Count > 0;
    public int LessonCount => Lessons.Count;
    public int TotalMinutes => Lessons.Sum(item => item.EstimatedMinutes);
}

public sealed record ContentLesson(
    string Slug,
    string Title,
    int Order,
    int EstimatedMinutes,
    string Summary,
    string Html,
    string PlainText,
    string ModuleSlug,
    string ModuleTitle,
    IReadOnlyList<LessonHeading> Headings,
    IReadOnlyList<QuizQuestion> Quiz)
{
    public string Url => $"/modulos/{ModuleSlug}/{Slug}";
    public bool HasQuiz => Quiz.Count > 0;
}

public sealed record SearchResult(ContentLesson Lesson, string Snippet, int Score);
