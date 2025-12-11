namespace SalesPosting.Worker
{
    public class ProcessingSettings
    {
        public int BatchSize { get; set; } = 100;
        public int PollingIntervalSeconds { get; set; } = 5;
        public int MaxDegreeOfParallelism { get; set; } = 4;
        public int MaxRetryCount { get; set; } = 3;
    }
}