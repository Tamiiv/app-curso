using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AppCurso.Web.Features.Account;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/cuenta");

        group.MapPost("/salir", async (
            [FromServices] SignInManager<AppUser> signInManager,
            [FromForm] string? returnUrl) =>
        {
            await signInManager.SignOutAsync();

            var target = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//", StringComparison.Ordinal)
                ? "/"
                : returnUrl;

            return TypedResults.LocalRedirect(target);
        }).RequireAuthorization();

        return endpoints;
    }
}
