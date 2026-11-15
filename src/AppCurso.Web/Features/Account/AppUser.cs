using Microsoft.AspNetCore.Identity;

namespace AppCurso.Web.Features.Account;

public sealed class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
