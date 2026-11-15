using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AppCurso.Tests.Integration;

public sealed class WebAppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WebAppSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HomeRendersThePublicLandingPage()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("De cero a profesional", html);
        Assert.Contains("name=\"color-scheme\"", html);
    }

    [Fact]
    public async Task ResponsesIncludeSecurityHeaders()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(
            "strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains(
            "frame-ancestors 'none'",
            response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task PublishedLessonRendersMarkdownAndNavigation()
    {
        var response = await _client.GetAsync("/modulos/poo/clases-objetos");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Clases y objetos", html);
        Assert.Contains("Siguiente", html);
    }

    [Fact]
    public async Task SitemapIncludesNewlyPublishedLessons()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/modulos/como-usar/progreso-marcadores", xml);
        Assert.Contains("/modulos/como-usar/como-practicar", xml);
        Assert.Contains("/modulos/poo/clases-objetos", xml);
        Assert.Contains("/privacidad", xml);
        Assert.Contains("/terminos", xml);
    }

    [Fact]
    public async Task ProfileRequiresAuthentication()
    {
        var response = await _client.GetAsync("/cuenta/perfil");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/cuenta/ingresar", response.Headers.Location.AbsolutePath);
        Assert.Contains("ReturnUrl", response.Headers.Location.Query);
    }

    [Fact]
    public async Task UnknownModuleReturnsNotFound()
    {
        var response = await _client.GetAsync("/modulos/modulo-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/privacidad", "Política de privacidad")]
    [InlineData("/terminos", "Términos de uso")]
    public async Task LegalPagesRender(string path, string heading)
    {
        var response = await _client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(heading, html);
        Assert.Contains("href=\"/privacidad\"", html);
        Assert.Contains("href=\"/terminos\"", html);
    }

    [Fact]
    public async Task HtmlResponsesAreCompressedWhenTheClientAcceptsIt()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.AcceptEncoding.ParseAdd("br, gzip");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentEncoding);
        Assert.Contains(
            response.Content.Headers.ContentEncoding,
            encoding => encoding is "br" or "gzip");
    }

    [Fact]
    public async Task HealthEndpointReportsHealthy()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
