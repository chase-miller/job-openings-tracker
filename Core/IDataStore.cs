using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker.Core
{
    public interface IDataStore
    {
        Task<DatabaseEntitiesV1> GetDatabaseEntities(CancellationToken token);
        Task MarkPostingAsProcessed(DatabaseEntitiesV1 entities, IEnumerable<JobPosting> processedPostings);
    }

    public class DatabaseEntitiesV1
    {
        public int Version { get; set; }
        public List<ProcessedPosting> AlreadyProcessedIds { get; set; } = new List<ProcessedPosting>();

        public DatabaseEntitiesV1()
        {
            Version = 1;
        }
    }

    public class ProcessedPosting
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }
}