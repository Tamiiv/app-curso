using System.Threading.RateLimiting;

using AppCurso.Web.Components;
using AppCurso.Web.Data;
using AppCurso.Web.Features.Account;
using AppCurso.Web.Features.Bookmarks;
using AppCurso.Web.Features.Content;
using AppCurso.Web.Features.Progress;
using AppCurso.Web.Features.Seo;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros proveedores) inyectan el puerto por variable de entorno.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<BookmarkService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailSender<AppUser>, AppEmailSender>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/cuenta/ingresar";
    options.LogoutPath = "/cuenta/salir";
    options.AccessDeniedPath = "/cuenta/acceso-denegado";
    options.Cookie.Name = "RutaNet.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(AuthRateLimiter.GetPartition);
});

builder.Services.AddHealthChecks();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
});

var connectionString = DatabaseConnection.Resolve(builder.Configuration);
if (connectionString is not null)
{
    void ConfigurePostgres(DbContextOptionsBuilder options) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());

    builder.Services.AddDbContextFactory<AppDbContext>(ConfigurePostgres, ServiceLifetime.Scoped);

    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<AppDbContext>();

    builder.Services
        .AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
}
else if (builder.Environment.IsDevelopment())
{
    // En desarrollo se puede trabajar sin PostgreSQL. Las cuentas y el progreso
    // viven en memoria y se pierden al reiniciar la aplicación.
    builder.Services.AddDbContextFactory<AppDbContext>(
        options => options.UseInMemoryDatabase("rutanet-dev"),
        ServiceLifetime.Scoped);
    builder.Services.AddDataProtection();
}
else
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión a PostgreSQL. Configurá ConnectionStrings:Default o DATABASE_URL.");
}

builder.Services.AddSingleton(_ =>
{
    var contentRoot = Path.Combine(AppContext.BaseDirectory, "content");
    return ContentLoader.Load(contentRoot);
});

var app = builder.Build();

if (connectionString is null)
{
    app.Logger.LogWarning(
        "No hay cadena de conexión a PostgreSQL. En desarrollo se usa una base en memoria.");
}
else if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    // Las migraciones se aplican solo cuando el entorno lo pide explícitamente.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    app.Logger.LogInformation("Aplicando migraciones de la base de datos...");
    await db.Database.MigrateAsync();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Content-Security-Policy"] = "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        return Task.CompletedTask;
    });

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseResponseCompression();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAccountEndpoints();
app.MapSitemap(app.Services.GetRequiredService<ContentCatalog>());

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program;
