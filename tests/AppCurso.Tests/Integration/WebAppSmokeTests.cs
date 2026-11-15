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

    [Fact]
    public async Task HealthEndpointReportsHealthy()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
