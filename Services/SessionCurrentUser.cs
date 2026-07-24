namespace LaptopTracker.Services
{
    public class SessionCurrentUser : ICurrentUser
    {
        public const string SessionKey = "ActorKid";

        private readonly IHttpContextAccessor _ctx;
        public SessionCurrentUser(IHttpContextAccessor ctx) => _ctx = ctx;

        public string? Kid => _ctx.HttpContext?.Session?.GetString(SessionKey);
        public bool IsIdentified => !string.IsNullOrWhiteSpace(Kid);
    }
}
