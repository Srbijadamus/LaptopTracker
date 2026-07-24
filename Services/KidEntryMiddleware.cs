namespace LaptopTracker.Services
{
    public class KidEntryMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly string[] _skipPrefixes = new[]
        {
            "/KidEntry",
            "/Localization/",
            "/Country/",
            "/Home/Error",
            "/Home/StatusCode",
            "/api/"
        };

        public KidEntryMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx)
        {
            var path = ctx.Request.Path.Value ?? "";

            var skip = _skipPrefixes.Any(p =>
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!skip)
            {
                await ctx.Session.LoadAsync();
                if (string.IsNullOrEmpty(ctx.Session.GetString("kid")))
                {
                    var returnUrl = ctx.Request.Path + ctx.Request.QueryString;
                    ctx.Response.Redirect($"/KidEntry?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }
            }

            await _next(ctx);
        }
    }
}
