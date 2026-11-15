using AppCurso.Tests.Support;
using AppCurso.Web.Domain;
using AppCurso.Web.Features.Bookmarks;
using AppCurso.Web.Features.Progress;

using Microsoft.EntityFrameworkCore;

namespace AppCurso.Tests.Features.Progress;

public sealed class ProgressServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordViewCreatesStartedProgress()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        await service.RecordViewAsync("user-1", "fundamentos", "que-es-dotnet");

        await using var db = factory.CreateDbContext();
        var progress = await db.LessonProgress.SingleAsync();

        Assert.Equal(ProgressStatus.Started, progress.Status);
        Assert.Equal(Now, progress.StartedAt);
        Assert.Equal(Now, progress.LastViewedAt);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public async Task SetCompletedIsIdempotent()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        Assert.True(await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", true));
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", true));

        await using var db = factory.CreateDbContext();
        var progress = await db.LessonProgress.SingleAsync();

        Assert.Equal(ProgressStatus.Completed, progress.Status);
        Assert.Equal(Now, progress.CompletedAt);
    }

    [Fact]
    public async Task UncompletingLessonClearsCompletedAt()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", true);
        clock.Advance(TimeSpan.FromMinutes(10));

        Assert.False(await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", false));

        await using var db = factory.CreateDbContext();
        var progress = await db.LessonProgress.SingleAsync();

        Assert.Equal(ProgressStatus.Started, progress.Status);
        Assert.Null(progress.CompletedAt);
        Assert.Equal(clock.GetUtcNow(), progress.LastViewedAt);
    }

    [Fact]
    public async Task SnapshotCountsOnlyCompletedLessonsFromTheCatalog()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", true);
        await service.SetCompletedAsync("user-1", "poo", "clases-objetos", true);

        await using (var db = factory.CreateDbContext())
        {
            db.LessonProgress.Add(new LessonProgress
            {
                UserId = "user-1",
                ModuleSlug = "modulo-inexistente",
                LessonSlug = "leccion-inexistente",
                Status = ProgressStatus.Completed,
                CompletedAt = Now
            });
            await db.SaveChangesAsync();
        }

        var snapshot = await service.GetSnapshotAsync("user-1");

        Assert.Equal(2, snapshot.CompletedLessons);
        Assert.Equal(TestContent.Catalog.AllLessons.Count, snapshot.TotalLessons);
        Assert.Equal(1, snapshot.Modules.Single(module => module.Module.Slug == "fundamentos").CompletedLessons);
        Assert.Equal(1, snapshot.Modules.Single(module => module.Module.Slug == "poo").CompletedLessons);
    }

    [Fact]
    public async Task ContinueReturnsFirstUnfinishedLessonAfterLastViewed()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        await service.SetCompletedAsync("user-1", "fundamentos", "que-es-dotnet", true);
        await service.RecordViewAsync("user-1", "fundamentos", "instalar-dotnet");

        var lesson = await service.GetContinueLessonAsync("user-1");

        Assert.NotNull(lesson);
        Assert.Equal("instalar-dotnet", lesson.Slug);
    }

    [Fact]
    public async Task ContinueStartsAtFirstUnfinishedLessonWithoutActivity()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var service = new ProgressService(factory, TestContent.Catalog, clock);

        var lesson = await service.GetContinueLessonAsync("user-without-progress");

        Assert.Equal(TestContent.Catalog.AllLessons[0].Slug, lesson?.Slug);
    }

    [Fact]
    public async Task LessonStateCombinesProgressAndBookmark()
    {
        var factory = new TestDbContextFactory();
        var clock = new TestTimeProvider(Now);
        var progressService = new ProgressService(factory, TestContent.Catalog, clock);
        var bookmarkService = new BookmarkService(factory, TestContent.Catalog, clock);

        await progressService.SetCompletedAsync("user-1", "poo", "clases-objetos", true);
        await bookmarkService.ToggleAsync("user-1", "poo", "clases-objetos");

        var state = await progressService.GetLessonStateAsync("user-1", "poo", "clases-objetos");

        Assert.True(state.IsCompleted);
        Assert.True(state.IsBookmarked);
        Assert.Equal(Now, state.CompletedAt);
    }
}
