using System.Text;
using System.Xml.Linq;

using AppCurso.Web.Features.Content;

namespace AppCurso.Web.Features.Seo;

public static class SitemapEndpoints
{
    public static IEndpointRouteBuilder MapSitemap(this IEndpointRouteBuilder endpoints, ContentCatalog catalog)
    {
        endpoints.MapGet("/sitemap.xml", (HttpContext context, IConfiguration configuration) =>
        {
            var baseUrl = ResolveBaseUrl(context, configuration);
            var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

            var urls = new List<string> { "/", "/modulos", "/buscar" };

            foreach (var module in catalog.Modules)
            {
                urls.Add($"/modulos/{module.Slug}");

                foreach (var lesson in module.Lessons)
                {
                    urls.Add(lesson.Url);
                }
            }

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset",
                    urls.Distinct(StringComparer.Ordinal).Select(path =>
                        new XElement(ns + "url",
                            new XElement(ns + "loc", $"{baseUrl}{path}")))));

            return Results.Text(
                document.ToString(SaveOptions.DisableFormatting),
                "application/xml",
                Encoding.UTF8);
        });

        endpoints.MapGet("/robots.txt", (HttpContext context, IConfiguration configuration) =>
        {
            var baseUrl = ResolveBaseUrl(context, configuration);
            var content = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Allow: /")
                .AppendLine("Disallow: /cuenta/")
                .AppendLine("Disallow: /health")
                .AppendLine()
                .AppendLine($"Sitemap: {baseUrl}/sitemap.xml")
                .ToString();

            return Results.Text(content, "text/plain", Encoding.UTF8);
        });

        return endpoints;
    }

    private static string ResolveBaseUrl(HttpContext context, IConfiguration configuration)
    {
        var configured = configuration["Site:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        return $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}".TrimEnd('/');
    }
}
