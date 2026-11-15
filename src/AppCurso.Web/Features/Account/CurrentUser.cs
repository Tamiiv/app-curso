using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace AppCurso.Web.Features.Account;

public sealed class CurrentUser(AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<ClaimsPrincipal> GetPrincipalAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var principal = await GetPrincipalAsync();
        return principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }

    public async Task<string?> GetDisplayNameAsync()
    {
        var principal = await GetPrincipalAsync();
        return principal.FindFirstValue(AppUserClaimsPrincipalFactory.DisplayNameClaimType)
            ?? principal.Identity?.Name;
    }
}
