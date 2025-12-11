using System;

namespace SalesPosting.Data.Entities
{
    public class SalesPayload
    {
        public long Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string TerminalId { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Processed, Failed
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedDate { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;

        // Navigation property
        public virtual ICollection<ProcessingError> ProcessingErrors { get; set; }
            = new List<ProcessingError>();
    }
}