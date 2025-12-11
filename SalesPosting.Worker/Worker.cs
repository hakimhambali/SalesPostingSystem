using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesPosting.Data.Data;
using SalesPosting.Data.Entities;
using System.Text.Json;

namespace SalesPosting.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ProcessingSettings _settings;

        public Worker(
            ILogger<Worker> logger,
            IServiceProvider serviceProvider,
            IOptions<ProcessingSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sales Processing Worker started at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Processing Settings - BatchSize: {batch}, Interval: {interval}s, Parallelism: {parallel}",
                _settings.BatchSize, _settings.PollingIntervalSeconds, _settings.MaxDegreeOfParallelism);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingSalesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in processing loop");
                }

                // Wait before next polling cycle
                await Task.Delay(
                    TimeSpan.FromSeconds(_settings.PollingIntervalSeconds),
                    stoppingToken);
            }

            _logger.LogInformation("Sales Processing Worker stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task ProcessPendingSalesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

            // Fetch pending records
            var pendingRecords = await context.SalesPayloads
                .Where(x => x.Status == "Pending")
                .OrderBy(x => x.CreatedDate)
                .Take(_settings.BatchSize)
                .ToListAsync(cancellationToken);

            if (!pendingRecords.Any())
            {
                // No pending records, skip this cycle
                return;
            }

            _logger.LogInformation("Processing {count} pending sales records", pendingRecords.Count);

            // Process records in parallel
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            var processedCount = 0;
            var failedCount = 0;

            await Parallel.ForEachAsync(pendingRecords, parallelOptions, async (record, ct) =>
            {
                // Create a new scope for each parallel task
                using var taskScope = _serviceProvider.CreateScope();
                var taskContext = taskScope.ServiceProvider.GetRequiredService<SalesDbContext>();

                var result = await ProcessSingleRecordAsync(record, taskContext, ct);

                if (result)
                    Interlocked.Increment(ref processedCount);
                else
                    Interlocked.Increment(ref failedCount);
            });

            _logger.LogInformation(
                "Batch processing completed. Processed: {processed}, Failed: {failed}",
                processedCount, failedCount);
        }

        private async Task<bool> ProcessSingleRecordAsync(
            SalesPayload record,
            SalesDbContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                // Attach the entity to this context
                context.SalesPayloads.Attach(record);

                _logger.LogDebug(
                    "Processing TransactionId: {transactionId}, Terminal: {terminal}",
                    record.TransactionId, record.TerminalId);

                // Update status to "Processing" to prevent other workers from picking it up
                record.Status = "Processing";
                await context.SaveChangesAsync(cancellationToken);

                // === ACTUAL BUSINESS LOGIC GOES HERE ===

                var payload = JsonSerializer.Deserialize<dynamic>(record.PayloadJson);

                // Simulate processing time (remove in production)
                await Task.Delay(100, cancellationToken);

                // Mark as processed
                record.Status = "Processed";
                record.ProcessedDate = DateTime.UtcNow;
                record.ErrorMessage = null;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully processed TransactionId: {transactionId}",
                    record.TransactionId);

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Processing cancelled for TransactionId: {transactionId}",
                    record.TransactionId);

                // Reset status back to Pending
                record.Status = "Pending";
                await context.SaveChangesAsync(CancellationToken.None);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing TransactionId: {transactionId}",
                    record.TransactionId);

                // Increment retry count
                record.RetryCount++;

                // Check if max retries exceeded
                if (record.RetryCount >= _settings.MaxRetryCount)
                {
                    record.Status = "Failed";
                    record.ErrorMessage = $"Max retries exceeded. Last error: {ex.Message}";

                    _logger.LogError(
                        "TransactionId: {transactionId} marked as Failed after {retries} retries",
                        record.TransactionId, record.RetryCount);
                }
                else
                {
                    // Reset to Pending for retry
                    record.Status = "Pending";
                    record.ErrorMessage = ex.Message;

                    _logger.LogWarning(
                        "TransactionId: {transactionId} will be retried. Attempt {attempt}/{max}",
                        record.TransactionId, record.RetryCount, _settings.MaxRetryCount);
                }

                // Log error to ProcessingError table
                try
                {
                    context.ProcessingErrors.Add(new ProcessingError
                    {
                        SalesPayloadId = record.Id,
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        OccurredAt = DateTime.UtcNow
                    });

                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log processing error to database");
                }

                return false;
            }
        }
    }
}