using LaptopTracker.Data;
using LaptopTracker.Models;

namespace LaptopTracker.Services
{
    public class LoginAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginAuditMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, LaptopTrackerDbContext db, ICurrentUser currentUser)
        {
            if (currentUser.IsIdentified)
            {
                await ctx.Session.LoadAsync();
                var kid = currentUser.Kid!;
                var sessionKey = $"audit_login_{kid}";
                var lastStr = ctx.Session.GetString(sessionKey);

                var needsLog = lastStr is null
                    || !DateTime.TryParse(lastStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var last)
                    || (DateTime.UtcNow - last).TotalMinutes > 30;

                if (needsLog)
                {
                    ctx.Session.SetString(sessionKey, DateTime.UtcNow.ToString("O"));
                    db.AuditLogs.Add(new AuditLog
                    {
                        Kid       = kid,
                        Action    = "Login",
                        EntityName = "",
                        EntityId  = "",
                        Source    = "Web",
                        IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                        Route     = ctx.Request.Path.Value,
                        RequestId = ctx.TraceIdentifier
                    });
                    await db.SaveChangesAsync();
                }
            }

            await _next(ctx);
        }
    }
}
