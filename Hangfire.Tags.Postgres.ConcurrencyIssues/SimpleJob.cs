using System.Threading;
using Hangfire.States;
using Hangfire.Tags.Attributes;
using Microsoft.Extensions.Logging;

namespace Hangfire.Console.Extensions.InitalizationIssueRepro
{
    public enum JobType
    {
        Type1,
        Type2,
        Type3,
        Type4,
        Type5,
        Type6,
    }

    public class BatchIdGenerator
    {
        public static BatchIdGenerator Instance { get; } = new BatchIdGenerator();
        private int current = 0;
        private object mutex = new object();

        public int GetNext()
        {
            lock (mutex)
            {
                current = current++ % 100;
            }

            return current;
        }
    }

    public class SimpleJob
    {
        private readonly ILogger<SimpleJob> _logger;
        private readonly IBackgroundJobClient _client;

        public SimpleJob(ILogger<SimpleJob> logger,
            IBackgroundJobClient client)
        {
            _logger = logger;
            _client = client;
        }

        [Tag("WorkerJob", "MyId-{0}", "BatchId-{1}","JobType-{2}")]
        public void RunWorkerJob(int id, int batchId, JobType jobType)
        {
            _logger.LogInformation("Processing {jobType} job {jobId} in batch {batchId}",
                jobType, id, batchId);
        }

        [Tag("GeneratorJob","JobType-{0}")]
        public void RunGeneratorJob(JobType jobType)
        {
            int batch = BatchIdGenerator.Instance.GetNext();

            for (int i = 0; i < 10_000; i++)
            {
                _client.Create<SimpleJob>(x => x.RunWorkerJob(i, batch, jobType), new EnqueuedState {Queue = "worker-queue"});
            }
        }
    }
}