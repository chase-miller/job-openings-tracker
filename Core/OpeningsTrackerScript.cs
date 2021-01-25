using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpeningsTracker.Core
{
    public class OpeningsTrackerScript
    {
        private readonly IList<IJobPostingSource> _postingSources;
        private readonly IList<IJobPostingNotifier> _notifiers;
        private readonly IDataStore _dataStore;
        private readonly ILogger<OpeningsTrackerScript> _logger;
        
        public OpeningsTrackerScript(IList<IJobPostingSource> postingSources, IList<IJobPostingNotifier> notifiers, IDataStore dataStore, ILogger<OpeningsTrackerScript> logger)
        {
            if (!postingSources.Any())
            {
                logger.LogCritical("No posting sources found. Have plugins been registered?");
                throw new ApplicationException("No posting sources found. Have plugins been registered?");
            }

            if (!notifiers.Any())
            {
                logger.LogCritical("No notifiers found. Have plugins been registered?");
                throw new ApplicationException("No notifiers found. Have plugins been registered?");
            }

            _postingSources = postingSources;
            _notifiers = notifiers;
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var databaseFile = await _dataStore.GetDatabaseEntities(cancellationToken);

            var newItems = (await FindNewJobPostings(databaseFile.AlreadyProcessedIds, cancellationToken))
                // .ExceptAlreadyProcessed(alreadyProcessedIds)
                .ToList();

            var notificationResults = await Notify(newItems, cancellationToken);

            var successes = notificationResults
                .Where(r => r.success)
                .Select(r => r.posting)
                .ToList();

            await _dataStore.MarkPostingAsProcessed(databaseFile, successes);

            var errors = notificationResults
                .Where(r => !r.success)
                .Select(r => (r.posting, r.ex))
                .ToList();

             ReportErrors(errors);
        }

        private async Task<List<JobPosting>> FindNewJobPostings(List<ProcessedPosting> alreadyProcessedPosting, CancellationToken cancellationToken)
        {
            var allNewPostings = new List<JobPosting>();

            foreach (var postingSource in _postingSources)
            {
                var newPostings = await FindNewJobPostings(postingSource, alreadyProcessedPosting, cancellationToken);
                allNewPostings.AddRange(newPostings);
            }

            return allNewPostings;
        }

        private async Task<List<JobPosting>> FindNewJobPostings(IJobPostingSource postingSource, List<ProcessedPosting> alreadyProcessedPosting, CancellationToken cancellationToken)
        {
            var alreadyProcessedIds = alreadyProcessedPosting
                .Where(id => id.SourceSystem == postingSource.PostingSourceId)
                .Select(id => id.Id);

            return (await postingSource.GetPostings(cancellationToken))
                .ExceptAlreadyProcessed(alreadyProcessedIds)
                .ToList();
        }

        private void ReportErrors(IList<(JobPosting posting, Exception ex)> errors)
        {
            if (!errors.Any())
                return;

            _logger.LogError("Failed to process postings with the following ids: {ids}", errors.Select(p => p.posting.Id));

            foreach (var (posting, exception) in errors)
            {
                _logger.LogError("Failed to process {id} due to {exception}", posting.Id, exception);
            }
        }

        private async Task<List<(JobPosting posting, bool success, Exception ex)>> Notify(IList<JobPosting> postings, CancellationToken cancellationToken)
        {
            if (!postings.Any())
            {
                _logger.LogInformation("No new postings discovered");
                return new List<(JobPosting posting, bool success, Exception ex)>();
            }

            var notificationResults = new List<(JobPosting posting, bool success, Exception ex)>();

            foreach (var notifier in _notifiers)
            {
                var result = await notifier.Notify(postings, cancellationToken);
                notificationResults.AddRange(result);
            }

            return notificationResults;
        }
    }

    static class LeverPostingExtensions
    {
        public static IEnumerable<JobPosting> ExceptAlreadyProcessed(this IEnumerable<JobPosting> postings,
            IEnumerable<string> alreadyProcessedIds)
        {
            return postings.ExceptBy(
                alreadyProcessedIds,
                posting => posting.Id,
                alreadyProcessedId => alreadyProcessedId
            );
        }
    }
}