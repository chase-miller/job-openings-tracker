using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.Core
{
    public interface IJobPostingSource
    {
        Task<List<JobPosting>> GetPostings(CancellationToken token);

        /// <summary>
        /// A UNIQUE identifier for a job posting source. I recommend using, prepending with, or appending with a guid.
        /// </summary>
        string PostingSourceId { get; }
    }
}
