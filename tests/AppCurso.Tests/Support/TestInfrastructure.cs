using AppCurso.Web.Data;
using AppCurso.Web.Features.Content;

using Microsoft.EntityFrameworkCore;

namespace AppCurso.Tests.Support;

public sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory(string? databaseName = null)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
    }

    public AppDbContext CreateDbContext() => new(_options);
}

public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan amount) => _now = _now.Add(amount);
}

public static class TestContent
{
    public static ContentCatalog Catalog { get; } = ContentLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "content"));
}
