using AppCurso.Web.Features.Account;

namespace AppCurso.Web.Domain;

public sealed class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public string ModuleSlug { get; set; } = string.Empty;
    public string LessonSlug { get; set; } = string.Empty;
    public ProgressStatus Status { get; set; } = ProgressStatus.Started;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastViewedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
