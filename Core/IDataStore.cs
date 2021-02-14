using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.Core
{
    public interface IDataStore
    {
        Task<List<ProcessedPosting>> GetProcessedPostings(Func<IQueryable<ProcessedPosting>, IQueryable<ProcessedPosting>> queryFunc, CancellationToken token);
        Task AddProcessedPostings(IEnumerable<ProcessedPosting> processedPostings, CancellationToken token);
    }
}