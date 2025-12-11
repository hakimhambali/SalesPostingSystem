using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesPosting.Data.Data;
using SalesPosting.Data.DTOs;
using SalesPosting.Data.Entities;
using System.Text.Json;

namespace SalesPosting.Api.Controllers
{
    [ApiController]
    [Route("api/sales")]
    public class SalesController : ControllerBase
    {
        private readonly SalesDbContext _context;
        private readonly ILogger<SalesController> _logger;

        public SalesController(SalesDbContext context, ILogger<SalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Post sales data from POS terminal
        /// </summary>
        [HttpPost("postsales")]
        public async Task<IActionResult> PostSales([FromBody] SalesPayloadDto payload)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(payload.TransactionId))
                {
                    return BadRequest(new { error = "TransactionId is required" });
                }

                if (string.IsNullOrWhiteSpace(payload.TerminalId))
                {
                    return BadRequest(new { error = "TerminalId is required" });
                }

                if (payload.Items == null || !payload.Items.Any())
                {
                    return BadRequest(new { error = "At least one item is required" });
                }

                // Check for duplicate transaction (Idempotency)
                var existingTransaction = await _context.SalesPayloads
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TransactionId == payload.TransactionId);

                if (existingTransaction != null)
                {
                    _logger.LogWarning(
                        "Duplicate transaction detected. TransactionId: {TransactionId}, TerminalId: {TerminalId}",
                        payload.TransactionId, payload.TerminalId);

                    return Conflict(new
                    {
                        error = "Transaction already exists",
                        transactionId = payload.TransactionId,
                        existingId = existingTransaction.Id,
                        status = existingTransaction.Status
                    });
                }

                // Serialize payload to JSON
                var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                // Create new sales payload record
                var salesPayload = new SalesPayload
                {
                    TransactionId = payload.TransactionId,
                    TerminalId = payload.TerminalId,
                    PayloadJson = payloadJson,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                _context.SalesPayloads.Add(salesPayload);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Sales posted successfully. Id: {Id}, TransactionId: {TransactionId}, TerminalId: {TerminalId}",
                    salesPayload.Id, salesPayload.TransactionId, salesPayload.TerminalId);

                return Ok(new
                {
                    message = "Sales posted successfully",
                    id = salesPayload.Id,
                    transactionId = salesPayload.TransactionId,
                    status = salesPayload.Status,
                    createdDate = salesPayload.CreatedDate
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while posting sales");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while posting sales");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        /// <summary>
        /// Get statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatistics()
        {
            var stats = await _context.SalesPayloads
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalRecords = await _context.SalesPayloads.CountAsync();

            return Ok(new
            {
                totalRecords,
                byStatus = stats
            });
        }

        /// <summary>
        /// Test endpoint to post a transaction that will fail processing
        /// </summary>
        [HttpPost("postsales/fail")]
        public async Task<IActionResult> PostSalesWithError([FromBody] SalesPayloadDto payload)
        {
            // Override the transaction ID to make it unique
            var failTransactionId = $"FAIL-{Guid.NewGuid()}";

            // Create a sales payload with invalid JSON to cause processing error
            var salesPayload = new SalesPayload
            {
                TransactionId = failTransactionId,
                TerminalId = payload.TerminalId,
                PayloadJson = "{ invalid json that will fail parsing }",  // This will cause JSON deserialization error
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            _context.SalesPayloads.Add(salesPayload);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Error test record created",
                id = salesPayload.Id,
                transactionId = failTransactionId,
                note = "This record will fail during processing"
            });
        }

        /// <summary>
        /// Bulk insert test data for performance testing
        /// </summary>
        [HttpPost("test/bulkinsert/{count}")]
        public async Task<IActionResult> BulkInsertTestData(int count)
        {
            if (count > 10000)
                return BadRequest("Max 10,000 records per request");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var records = new List<SalesPayload>();

            for (int i = 0; i < count; i++)
            {
                records.Add(new SalesPayload
                {
                    TransactionId = $"PERF-TEST-{Guid.NewGuid()}",
                    TerminalId = $"POS-{(i % 10) + 1:D3}", // Distribute across 10 terminals
                    PayloadJson = "{\"amount\": 100.00}",
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SalesPayloads.AddRangeAsync(records);
            await _context.SaveChangesAsync();

            stopwatch.Stop();

            return Ok(new
            {
                recordsInserted = count,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                recordsPerSecond = (count / stopwatch.Elapsed.TotalSeconds).ToString("N0")
            });
        }

        /// <summary>
        /// Clean up test data
        /// </summary>
        [HttpDelete("test/cleanup")]
        public async Task<IActionResult> CleanupTestData()
        {
            var testRecords = await _context.SalesPayloads
                .Where(x => x.TransactionId.StartsWith("PERF-TEST-"))
                .ToListAsync();

            _context.SalesPayloads.RemoveRange(testRecords);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Test data cleaned up",
                recordsDeleted = testRecords.Count
            });
        }
    }
}