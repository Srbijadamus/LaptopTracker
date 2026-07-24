namespace LaptopTracker.Services
{
    public class WindowsCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _ctx;

        public WindowsCurrentUser(IHttpContextAccessor ctx) => _ctx = ctx;

        public string? Kid
        {
            get
            {
                var name = _ctx.HttpContext?.User?.Identity?.Name;   // "U-DOM1\S69307"
                if (string.IsNullOrWhiteSpace(name)) return null;
                var i = name.LastIndexOf('\\');
                return (i >= 0 ? name[(i + 1)..] : name).ToUpperInvariant();   // → "S69307"
            }
        }

        public bool IsIdentified => Kid is not null;
    }
}
