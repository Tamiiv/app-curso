using AppCurso.Web.Features.Account;

namespace AppCurso.Web.Domain;

public sealed class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public string ModuleSlug { get; set; } = string.Empty;
    public string LessonSlug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
