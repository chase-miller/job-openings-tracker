using System.Collections.Generic;

namespace OpeningsTracker.Core.Models
{
    public class DatabaseEntitiesV1
    {
        public int Version { get; set; }
        public List<ProcessedPosting> AlreadyProcessedIds { get; set; } = new List<ProcessedPosting>();

        public DatabaseEntitiesV1()
        {
            Version = 1;
        }
    }
}