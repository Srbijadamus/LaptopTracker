namespace LaptopTracker.Services
{
    public class SessionCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _ctx;
        public SessionCurrentUser(IHttpContextAccessor ctx) => _ctx = ctx;

        public string? Kid => _ctx.HttpContext?.Session.GetString("kid");
        public bool IsIdentified => !string.IsNullOrEmpty(Kid);
    }
}
