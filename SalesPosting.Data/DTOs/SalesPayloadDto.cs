using System;
using System.Collections.Generic;

namespace SalesPosting.Data.DTOs
{
    public class SalesPayloadDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string TerminalId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerName { get; set; }
        public string? PaymentMethod { get; set; }
        public List<SalesItemDto> Items { get; set; } = new();
    }
}