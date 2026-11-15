using AppCurso.Web.Features.Content;

namespace AppCurso.Web.Features.Progress;

public sealed record ModuleProgress(ContentModule Module, int CompletedLessons)
{
    public int TotalLessons => Module.LessonCount;
    public int Percent => TotalLessons == 0 ? 0 : (int)Math.Round(CompletedLessons * 100d / TotalLessons);
    public bool IsComplete => TotalLessons > 0 && CompletedLessons == TotalLessons;
}

public sealed record ProgressSnapshot(int CompletedLessons, int TotalLessons, IReadOnlyList<ModuleProgress> Modules)
{
    public int Percent => TotalLessons == 0 ? 0 : (int)Math.Round(CompletedLessons * 100d / TotalLessons);
    public bool HasProgress => CompletedLessons > 0;
}

public sealed record LessonProgressState(bool IsCompleted, bool IsBookmarked, DateTimeOffset? CompletedAt)
{
    public static readonly LessonProgressState Empty = new(false, false, null);
}
