namespace AppCurso.Web.Features.Content;

internal sealed class ModuleManifest
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Status { get; init; } = "planned";
    public string[] Topics { get; init; } = [];
}
