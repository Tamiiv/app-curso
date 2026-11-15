using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace AppCurso.Web.Features.Account;

public sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "RutaNet.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(10)
    };

    public void RedirectTo(string? uri)
    {
        uri ??= "/";

        // Evita redirecciones abiertas: solo se aceptan rutas locales.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative) || uri.StartsWith("//", StringComparison.Ordinal))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
            uri = $"/{uri.TrimStart('/')}";
        }

        navigationManager.NavigateTo(uri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);

    public void RedirectToInvalidUser(UserManager<AppUser> userManager, HttpContext context)
        => RedirectToWithStatus(
            "/cuenta/ingresar",
            $"Error: no pudimos cargar el usuario con ID '{userManager.GetUserId(context.User)}'.",
            context);
}
