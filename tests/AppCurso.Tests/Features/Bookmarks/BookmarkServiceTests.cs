using AppCurso.Tests.Support;
using AppCurso.Web.Domain;
using AppCurso.Web.Features.Bookmarks;

using Microsoft.EntityFrameworkCore;

namespace AppCurso.Tests.Features.Bookmarks;

public sealed class BookmarkServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ToggleAddsAndRemovesBookmark()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new BookmarkService(factory, TestContent.Catalog, clock);

        Assert.True(await service.ToggleAsync("user-1", "poo", "clases-objetos"));
        Assert.True(await service.IsBookmarkedAsync("user-1", "poo", "clases-objetos"));

        Assert.False(await service.ToggleAsync("user-1", "poo", "clases-objetos"));
        Assert.False(await service.IsBookmarkedAsync("user-1", "poo", "clases-objetos"));

        await using var db = factory.CreateDbContext();
        Assert.Empty(await db.Bookmarks.ToArrayAsync());
    }

    [Fact]
    public async Task ToggleIgnoresLessonsOutsideTheCatalog()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new BookmarkService(factory, TestContent.Catalog, clock);

        Assert.False(await service.ToggleAsync("user-1", "no-existe", "tampoco"));

        await using var db = factory.CreateDbContext();
        Assert.Empty(await db.Bookmarks.ToArrayAsync());
    }

    [Fact]
    public async Task BookmarkedLessonsAreReturnedNewestFirstAndSkipStaleEntries()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new BookmarkService(factory, TestContent.Catalog, clock);

        await service.ToggleAsync("user-1", "fundamentos", "que-es-dotnet");
        clock.Advance(TimeSpan.FromMinutes(5));
        await service.ToggleAsync("user-1", "poo", "clases-objetos");

        await using (var db = factory.CreateDbContext())
        {
            db.Bookmarks.Add(new Bookmark
            {
                UserId = "user-1",
                ModuleSlug = "modulo-inexistente",
                LessonSlug = "leccion-inexistente",
                CreatedAt = clock.GetUtcNow().AddMinutes(1)
            });
            await db.SaveChangesAsync();
        }

        var lessons = await service.GetBookmarkedLessonsAsync("user-1");

        Assert.Equal(["clases-objetos", "que-es-dotnet"], lessons.Select(lesson => lesson.Slug));
    }
}
