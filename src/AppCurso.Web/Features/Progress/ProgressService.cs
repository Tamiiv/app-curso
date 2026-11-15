using AppCurso.Web.Data;
using AppCurso.Web.Domain;
using AppCurso.Web.Features.Content;

using Microsoft.EntityFrameworkCore;

namespace AppCurso.Web.Features.Progress;

public sealed class ProgressService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ContentCatalog catalog,
    TimeProvider timeProvider)
{
    public async Task<LessonProgressState> GetLessonStateAsync(
        string userId,
        string moduleSlug,
        string lessonSlug,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = await db.LessonProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
                cancellationToken);

        var isBookmarked = await db.Bookmarks
            .AsNoTracking()
            .AnyAsync(
                row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
                cancellationToken);

        return new LessonProgressState(
            progress?.Status == ProgressStatus.Completed,
            isBookmarked,
            progress?.CompletedAt);
    }

    public async Task RecordViewAsync(
        string userId,
        string moduleSlug,
        string lessonSlug,
        CancellationToken cancellationToken = default)
    {
        if (catalog.GetLesson(moduleSlug, lessonSlug) is null)
        {
            return;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = await db.LessonProgress.FirstOrDefaultAsync(
            row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
            cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (progress is null)
        {
            db.LessonProgress.Add(new LessonProgress
            {
                UserId = userId,
                ModuleSlug = moduleSlug,
                LessonSlug = lessonSlug,
                Status = ProgressStatus.Started,
                StartedAt = now,
                LastViewedAt = now
            });
        }
        else
        {
            progress.LastViewedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SetCompletedAsync(
        string userId,
        string moduleSlug,
        string lessonSlug,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        if (catalog.GetLesson(moduleSlug, lessonSlug) is null)
        {
            return false;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = await db.LessonProgress.FirstOrDefaultAsync(
            row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
            cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (progress is null)
        {
            progress = new LessonProgress
            {
                UserId = userId,
                ModuleSlug = moduleSlug,
                LessonSlug = lessonSlug,
                StartedAt = now,
                LastViewedAt = now
            };
            db.LessonProgress.Add(progress);
        }

        progress.Status = completed ? ProgressStatus.Completed : ProgressStatus.Started;

        if (completed && progress.CompletedAt is null)
        {
            progress.CompletedAt = now;
        }
        else if (!completed)
        {
            progress.CompletedAt = null;
        }

        progress.LastViewedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return completed;
    }

    public async Task<ProgressSnapshot> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var completed = await db.LessonProgress
            .AsNoTracking()
            .Where(row => row.UserId == userId && row.Status == ProgressStatus.Completed)
            .Select(row => new { row.ModuleSlug, row.LessonSlug })
            .ToArrayAsync(cancellationToken);

        var completedKeys = completed
            .Select(row => BuildKey(row.ModuleSlug, row.LessonSlug))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var completedLessonCount = catalog.AllLessons.Count(lesson =>
            completedKeys.Contains(BuildKey(lesson.ModuleSlug, lesson.Slug)));

        var modules = new List<ModuleProgress>(catalog.Modules.Count);

        foreach (var module in catalog.Modules)
        {
            var completedLessons = module.Lessons.Count(lesson => completedKeys.Contains(BuildKey(module.Slug, lesson.Slug)));
            modules.Add(new ModuleProgress(module, completedLessons));
        }

        return new ProgressSnapshot(
            completedLessonCount,
            catalog.AllLessons.Count,
            modules);
    }

    public async Task<ContentLesson?> GetContinueLessonAsync(string userId, CancellationToken cancellationToken = default)
    {
        var lessons = catalog.AllLessons;
        if (lessons.Count == 0)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var completedKeys = (await db.LessonProgress
                .AsNoTracking()
                .Where(row => row.UserId == userId && row.Status == ProgressStatus.Completed)
                .Select(row => new { row.ModuleSlug, row.LessonSlug })
                .ToArrayAsync(cancellationToken))
            .Select(row => BuildKey(row.ModuleSlug, row.LessonSlug))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lastViewed = await db.LessonProgress
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .OrderByDescending(row => row.LastViewedAt)
            .Select(row => new { row.ModuleSlug, row.LessonSlug })
            .FirstOrDefaultAsync(cancellationToken);

        if (lastViewed is not null)
        {
            var lastViewedIndex = -1;
            for (var index = 0; index < lessons.Count; index++)
            {
                if (string.Equals(lessons[index].ModuleSlug, lastViewed.ModuleSlug, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(lessons[index].Slug, lastViewed.LessonSlug, StringComparison.OrdinalIgnoreCase))
                {
                    lastViewedIndex = index;
                    break;
                }
            }

            if (lastViewedIndex >= 0)
            {
                for (var index = lastViewedIndex; index < lessons.Count; index++)
                {
                    if (!completedKeys.Contains(BuildKey(lessons[index].ModuleSlug, lessons[index].Slug)))
                    {
                        return lessons[index];
                    }
                }
            }
        }

        return lessons.FirstOrDefault(lesson => !completedKeys.Contains(BuildKey(lesson.ModuleSlug, lesson.Slug)))
            ?? lessons[0];
    }

    private static string BuildKey(string moduleSlug, string lessonSlug) => $"{moduleSlug}/{lessonSlug}";
}
