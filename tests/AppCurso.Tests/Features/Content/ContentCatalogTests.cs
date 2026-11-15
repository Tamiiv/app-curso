using AppCurso.Web.Features.Content;

namespace AppCurso.Tests.Features.Content;

public sealed class ContentCatalogTests
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

        Assert.Equal(5, publishedModules.Length);
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

        Assert.Equal(8, plannedModules.Length);
        Assert.All(plannedModules, module =>
        {
            Assert.Empty(module.Lessons);
            Assert.NotEmpty(module.Topics);
        });
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
}
