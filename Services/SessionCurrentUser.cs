namespace LaptopTracker.Services
{
    public class SessionCurrentUser : ICurrentUser
    {
        public const string SessionKey = "ActorKid";

        private readonly IHttpContextAccessor _ctx;
        public SessionCurrentUser(IHttpContextAccessor ctx) => _ctx = ctx;

        public string? Kid
        {
            get
            {
                var ctx = _ctx.HttpContext;
                if (ctx is null) return null;
                if (ctx.Items.TryGetValue(SessionKey, out var item) && item is string agentKid)
                    return agentKid;
                return ctx.Session?.GetString(SessionKey);
            }
        }
        public bool IsIdentified => !string.IsNullOrWhiteSpace(Kid);
    }
}
