using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AppCurso.Tests.Integration;

public sealed partial class AccountFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserCanRegisterLoginAndLogout()
    {
        var email = $"student-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        using var registrationClient = CreateClient();
        var registrationToken = await GetAntiforgeryTokenAsync(registrationClient, "/cuenta/registro");

        var registration = await registrationClient.PostAsync(
            "/cuenta/registro",
            Form(
                ("_handler", "register"),
                ("__RequestVerificationToken", registrationToken),
                ("ReturnUrlForm", string.Empty),
                ("Input.DisplayName", "Estudiante de Prueba"),
                ("Input.Email", email),
                ("Input.Password", password),
                ("Input.ConfirmPassword", password)));

        Assert.Equal(HttpStatusCode.Redirect, registration.StatusCode);
        Assert.Equal("/cuenta/perfil", LocationPath(registration.Headers.Location));

        var profile = await registrationClient.GetAsync("/cuenta/perfil");
        var profileHtml = await profile.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Contains(email, profileHtml);
        Assert.Contains("Estudiante de Prueba", profileHtml);

        var logoutToken = ExtractAntiforgeryToken(profileHtml);
        var logout = await registrationClient.PostAsync(
            "/cuenta/salir",
            Form(
                ("__RequestVerificationToken", logoutToken),
                ("returnUrl", "/")));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/", LocationPath(logout.Headers.Location));

        using var loginClient = CreateClient();
        var loginToken = await GetAntiforgeryTokenAsync(loginClient, "/cuenta/ingresar");
        var login = await loginClient.PostAsync(
            "/cuenta/ingresar",
            Form(
                ("_handler", "login"),
                ("__RequestVerificationToken", loginToken),
                ("ReturnUrlForm", string.Empty),
                ("Input.Email", email),
                ("Input.Password", password)));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/cuenta/perfil", LocationPath(login.Headers.Location));

        var authenticatedProfile = await loginClient.GetAsync("/cuenta/perfil");
        var authenticatedHtml = await authenticatedProfile.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, authenticatedProfile.StatusCode);
        Assert.Contains(email, authenticatedHtml);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private static string? LocationPath(Uri? location) =>
        location is null ? null : location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] values) =>
        new(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value)));

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return ExtractAntiforgeryToken(html);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);
        Assert.True(match.Success, "No se encontró el token antiforgery en el HTML.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryTokenPattern();
}
