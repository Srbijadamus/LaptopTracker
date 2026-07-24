using Microsoft.AspNetCore.Http;

namespace LaptopTracker.Services
{
    public class AgentApiKeyMiddleware
    {
        private const string HeaderName  = "X-Api-Key";
        private const string AgentKid    = "SVC-AGENT";
        private const string ItemsKey    = "ActorKid";
        private readonly RequestDelegate _next;
        private readonly string?         _expectedKey;

        public AgentApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next        = next;
            _expectedKey = config["AgentApiKey"];
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (!ctx.Request.Path.StartsWithSegments("/api"))
            {
                await _next(ctx);
                return;
            }

            if (string.IsNullOrEmpty(_expectedKey))
            {
                ctx.Response.StatusCode  = 503;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"API key not configured\"}");
                return;
            }

            if (!ctx.Request.Headers.TryGetValue(HeaderName, out var supplied) ||
                !string.Equals(supplied, _expectedKey, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
                return;
            }

            ctx.Items[ItemsKey] = AgentKid;
            await _next(ctx);
        }
    }
}
