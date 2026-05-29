using System.ComponentModel.DataAnnotations;

namespace LaptopTracker.Models.Agent
{
    public class IdempotencyRecord
    {
        public int Id { get; set; }
        [Required, MaxLength(128)] public string RequestId { get; set; } = string.Empty;
        [Required, MaxLength(128)] public string Endpoint { get; set; } = string.Empty;
        [Required] public string ResponseJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentActionAudit
    {
        public int Id { get; set; }
        [Required, MaxLength(128)] public string RequestId { get; set; } = string.Empty;
        [Required, MaxLength(128)] public string Endpoint { get; set; } = string.Empty;
        [Required, MaxLength(64)] public string Source { get; set; } = string.Empty;
        public int TargetId { get; set; }
        [Required] public string ActionJson { get; set; } = string.Empty;
        [Required] public string Result { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
