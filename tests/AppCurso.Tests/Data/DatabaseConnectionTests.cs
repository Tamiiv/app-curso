using AppCurso.Web.Data;

using Npgsql;

namespace AppCurso.Tests.Data;

public sealed class DatabaseConnectionTests
{
    [Fact]
    public void ConvertsPostgresUriToNpgsqlConnectionString()
    {
        const string uri =
            "postgresql://neondb_owner:s3cret%21@ep-cool-123.us-east-2.aws.neon.tech/neondb" +
            "?sslmode=require&channel_binding=require&application_name=ruta-dotnet";

        var connectionString = DatabaseConnection.Normalize(uri);
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("ep-cool-123.us-east-2.aws.neon.tech", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("neondb_owner", builder.Username);
        Assert.Equal("s3cret!", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(ChannelBinding.Require, builder.ChannelBinding);
        Assert.Equal("ruta-dotnet", builder.ApplicationName);
    }

    [Fact]
    public void KeepsKeyValueConnectionStringsUntouched()
    {
        const string connectionString = "Host=localhost;Database=ruta;Username=postgres;Password=postgres";

        Assert.Equal(connectionString, DatabaseConnection.Normalize(connectionString));
    }
}
