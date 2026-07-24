using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LaptopTracker.Services
{
    public class RequireKidFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var anon = ctx.ActionDescriptor.EndpointMetadata
                .OfType<AllowAnonymousAttribute>().Any();

            // Agent API routes use their own API key scheme — never redirect them
            var path = ctx.HttpContext.Request.Path.Value ?? "";
            var isAgentApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

            if (!anon && !isAgentApi)
            {
                var user = ctx.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
                if (!user.IsIdentified)
                {
                    var returnUrl = ctx.HttpContext.Request.Path + ctx.HttpContext.Request.QueryString;
                    ctx.Result = new RedirectToActionResult("Login", "Identity",
                        new { returnUrl = returnUrl.ToString() });
                    return;
                }
            }

            await next();
        }
    }
}
