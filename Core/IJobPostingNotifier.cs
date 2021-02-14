using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.Core
{
    public interface IJobPostingNotifier
    {
        Task<IEnumerable<(JobPosting posting, bool success, Exception ex)>> Notify(IEnumerable<JobPosting> postings, CancellationToken cancellationToken);
        string NotifierId { get; }
    }
}
