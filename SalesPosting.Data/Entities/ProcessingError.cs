using System;

namespace SalesPosting.Data.Entities
{
    public class ProcessingError
    {
        public long Id { get; set; }
        public long SalesPayloadId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual SalesPayload SalesPayload { get; set; } = null!;
    }
}