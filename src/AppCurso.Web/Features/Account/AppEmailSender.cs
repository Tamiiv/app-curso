using System.Net.Http.Json;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Identity;

namespace AppCurso.Web.Features.Account;

public sealed class AppEmailSender(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<AppEmailSender> logger)
    : IEmailSender<AppUser>
{
    private const string ResendEndpoint = "https://api.resend.com/emails";

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) =>
        SendAsync(
            email,
            "Confirmá tu cuenta de Ruta .NET",
            $"<p>Hola {HtmlEncoder.Default.Encode(user.DisplayName ?? email)}:</p>" +
            $"<p>Confirmá tu cuenta haciendo clic en <a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">este enlace</a>.</p>",
            confirmationLink);

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) =>
        SendAsync(
            email,
            "Restablecé tu contraseña de Ruta .NET",
            $"<p>Hola {HtmlEncoder.Default.Encode(user.DisplayName ?? email)}:</p>" +
            $"<p>Recibimos un pedido para restablecer tu contraseña. Podés hacerlo desde " +
            $"<a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">este enlace</a>. Si no fuiste vos, ignorá este mensaje.</p>",
            resetLink);

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) =>
        SendAsync(
            email,
            "Código para restablecer tu contraseña",
            $"<p>Tu código para restablecer la contraseña es <strong>{HtmlEncoder.Default.Encode(resetCode)}</strong>.</p>",
            resetCode);

    private async Task SendAsync(string email, string subject, string html, string fallbackLink)
    {
        var apiKey = configuration["Email:ResendApiKey"];
        var from = configuration["Email:From"] ?? "Ruta .NET <no-reply@rutanet.dev>";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Email no configurado. Enlace para {Email}: {Link}",
                    email,
                    fallbackLink);
            }
            else
            {
                logger.LogError("Email no configurado: falta Email:ResendApiKey. No se pudo enviar el mensaje a {Email}.", email);
            }

            return;
        }

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { email },
                subject,
                html
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError(
                    "Resend respondió {StatusCode} al enviar un email a {Email}: {Body}",
                    (int)response.StatusCode,
                    email,
                    body);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No se pudo enviar el email a {Email}.", email);
        }
    }
}
