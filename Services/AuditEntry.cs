using System.Text.Json;
using LaptopTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LaptopTracker.Services
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry) => Entry = entry;

        public EntityEntry Entry { get; }
        public string Kid = "";
        public string Action = "";
        public string EntityName = "";
        public string EntityId = "";
        public string Source = "Web";
        public string? EntityLabel;
        public string? IpAddress;
        public string? Route;
        public string? RequestId;
        public Dictionary<string, object?> OldValues = new();
        public Dictionary<string, object?> NewValues = new();
        public List<PropertyEntry> TemporaryKeys = new();

        public void ResolveGeneratedKeys()
        {
            foreach (var p in TemporaryKeys)
                EntityId = p.CurrentValue?.ToString() ?? "";
        }

        public AuditLog ToLog()
        {
            var fields = new Dictionary<string, object>();
            foreach (var key in OldValues.Keys.Union(NewValues.Keys))
                fields[key] = new
                {
                    old  = Redact(key, OldValues.GetValueOrDefault(key)),
                    @new = Redact(key, NewValues.GetValueOrDefault(key))
                };

            return new AuditLog
            {
                Kid         = Kid,
                Action      = Action,
                EntityName  = EntityName,
                EntityId    = EntityId,
                EntityLabel = EntityLabel,
                Changes     = fields.Count > 0 ? JsonSerializer.Serialize(fields) : null,
                IpAddress   = IpAddress,
                Route       = Route,
                Source      = Source,
                RequestId   = RequestId,
                TimestampUtc = DateTime.UtcNow
            };
        }

        private static readonly HashSet<string> Sensitive =
            new(StringComparer.OrdinalIgnoreCase) { "UserAddress", "Acknowledgement", "Signature" };

        private static object? Redact(string field, object? value) =>
            value is null ? null : Sensitive.Contains(field) ? "[redacted]" : value;
    }
}
