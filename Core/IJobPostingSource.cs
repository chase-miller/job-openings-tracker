using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker.Core
{
    public interface IJobPostingSource
    {
        Task<List<JobPosting>> GetPostings(CancellationToken token);
        string PostingSourceId { get; }
    }
}
