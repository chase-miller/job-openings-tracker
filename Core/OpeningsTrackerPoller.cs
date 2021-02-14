using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.Core
{
    public class OpeningsTrackerPoller
    {
        private readonly IList<IJobPostingSource> _postingSources;
        private readonly IList<IJobPostingNotifier> _notifiers;
        private readonly IDataStore _dataStore;
        private readonly ILogger<OpeningsTrackerPoller> _logger;

        public OpeningsTrackerPoller(IList<IJobPostingSource> postingSources, IList<IJobPostingNotifier> notifiers, IDataStore dataStore, ILogger<OpeningsTrackerPoller> logger)
        {
            _postingSources = postingSources ?? throw new ArgumentNullException(nameof(postingSources));
            _notifiers = notifiers ?? throw new ArgumentNullException(nameof(notifiers));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // TODO - don't pull back everything from the datastore...only what we need (hit the DB after getting new postings)
            var alreadyProcessed = await _dataStore.GetProcessedPostings(postings => postings, cancellationToken);

            var newItems = await FindNewJobPostings(alreadyProcessed, cancellationToken);

            // TODO - delete me!!
            newItems = newItems.Take(1).ToList();

            var notificationResults = await Notify(newItems, cancellationToken);

            // For now a given posting's success from any notification system is a success for all. Consider changing this.
            var successes = notificationResults
                .Where(r => r.success)
                .Select(r => r.posting)
                .DistinctBy(posting => posting.Id)            // prevent duplicates as a result of multiple notifiers return results
                .ToList();
            
            var converted = successes
                .Select(p => new ProcessedPosting
                {
                    Id = p.Id,
                    SourceSystem = p.SourceSystemId
                });

            await _dataStore.AddProcessedPostings(converted, cancellationToken);

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
                var results = await notifier.Notify(postings, cancellationToken);
                notificationResults.AddRange(results);
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