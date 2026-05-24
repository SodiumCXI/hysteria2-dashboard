using Hysteria2Dashboard.Application.Interfaces;

namespace Hysteria2Dashboard.API.Middleware;

public class RouteSaltMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppConfigStore appConfigStore)
    {
        var path = context.Request.Path.Value ?? "";

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var routeSalt = await appConfigStore.GetRouteSaltAsync();
            var clientSalt = context.Request.Headers["X-Route-Salt"].FirstOrDefault();

            if (clientSalt != routeSalt)
            {
                context.Response.StatusCode = 444;
                return;
            }
        }

        await next(context);
    }
}
