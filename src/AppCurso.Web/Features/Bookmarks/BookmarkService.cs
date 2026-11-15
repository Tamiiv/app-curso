using AppCurso.Web.Data;
using AppCurso.Web.Domain;
using AppCurso.Web.Features.Content;

using Microsoft.EntityFrameworkCore;

namespace AppCurso.Web.Features.Bookmarks;

public sealed class BookmarkService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ContentCatalog catalog,
    TimeProvider timeProvider)
{
    public async Task<bool> IsBookmarkedAsync(
        string userId,
        string moduleSlug,
        string lessonSlug,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Bookmarks
            .AsNoTracking()
            .AnyAsync(
                row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
                cancellationToken);
    }

    public async Task<bool> ToggleAsync(
        string userId,
        string moduleSlug,
        string lessonSlug,
        CancellationToken cancellationToken = default)
    {
        if (catalog.GetLesson(moduleSlug, lessonSlug) is null)
        {
            return false;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var bookmark = await db.Bookmarks.FirstOrDefaultAsync(
            row => row.UserId == userId && row.ModuleSlug == moduleSlug && row.LessonSlug == lessonSlug,
            cancellationToken);

        if (bookmark is not null)
        {
            db.Bookmarks.Remove(bookmark);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        db.Bookmarks.Add(new Bookmark
        {
            UserId = userId,
            ModuleSlug = moduleSlug,
            LessonSlug = lessonSlug,
            CreatedAt = timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ContentLesson>> GetBookmarkedLessonsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var bookmarks = await db.Bookmarks
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new { row.ModuleSlug, row.LessonSlug })
            .ToArrayAsync(cancellationToken);

        var lessons = new List<ContentLesson>(bookmarks.Length);

        foreach (var bookmark in bookmarks)
        {
            if (catalog.GetLesson(bookmark.ModuleSlug, bookmark.LessonSlug) is { } lesson)
            {
                lessons.Add(lesson);
            }
        }

        return lessons;
    }
}
