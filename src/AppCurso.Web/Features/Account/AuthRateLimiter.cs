using System.Threading.RateLimiting;

namespace AppCurso.Web.Features.Account;

public static class AuthRateLimiter
{
    public static RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var isAuthPost = HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.StartsWithSegments("/cuenta");

        if (!isAuthPost)
        {
            return RateLimitPartition.GetNoLimiter("none");
        }

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 15,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}
