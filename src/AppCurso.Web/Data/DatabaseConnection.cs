using Npgsql;

namespace AppCurso.Web.Data;

public static class DatabaseConnection
{
    private const int DefaultPostgresPort = 5432;

    public static string? Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Normalize(configured);
        }

        var databaseUrl = configuration["DATABASE_URL"];
        return string.IsNullOrWhiteSpace(databaseUrl) ? null : Normalize(databaseUrl);
    }

    public static string Normalize(string value)
    {
        var trimmed = value.Trim();
        return IsPostgresUri(trimmed) ? FromUri(trimmed) : trimmed;
    }

    private static bool IsPostgresUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string FromUri(string value)
    {
        var uri = new Uri(value, UriKind.Absolute);
        var credentials = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPostgresPort,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        foreach (var (key, parameterValue) in ParseQuery(uri.Query))
        {
            switch (key.ToLowerInvariant())
            {
                case "sslmode" when Enum.TryParse<SslMode>(parameterValue, ignoreCase: true, out var sslMode):
                    builder.SslMode = sslMode;
                    break;
                case "channel_binding" when Enum.TryParse<ChannelBinding>(parameterValue, ignoreCase: true, out var channelBinding):
                    builder.ChannelBinding = channelBinding;
                    break;
                case "application_name":
                    builder.ApplicationName = parameterValue;
                    break;
            }
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]);
            var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            yield return (key, value);
        }
    }
}
