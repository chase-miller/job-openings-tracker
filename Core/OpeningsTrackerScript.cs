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
        private readonly IJobPostingSource _postingSource;
        private readonly Database _database;
        private readonly OpeningsTrackerScriptConfig _config;
        private readonly ILogger<OpeningsTrackerScript> _logger;

        public OpeningsTrackerScript(IJobPostingSource postingSource, Database database, OpeningsTrackerScriptConfig config, ILogger<OpeningsTrackerScript> logger)
        {
            _postingSource = postingSource ?? throw new ArgumentNullException(nameof(postingSource));
            _database = database;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var databaseFile = await _database.GetDatabaseFile(cancellationToken);

            var alreadyProcessedIds = databaseFile.AlreadyProcessedIds
                .Where(id => id.SourceSystem == _postingSource.PostingSourceId)
                .Select(id => id.Id);

            var newItems = (await _postingSource.GetPostings(cancellationToken))
                .ExceptAlreadyProcessed(alreadyProcessedIds)
                .ToList();

            var processResults = await Process(newItems);

            var successes = processResults
                .Where(r => r.success)
                .Select(r => r.posting)
                .ToList();

            await _database.MarkPostingAsProcessed(databaseFile, successes);

            var errors = processResults
                .Where(r => !r.success)
                .Select(r => (r.posting, r.ex))
                .ToList();

             ReportErrors(errors);
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

        private async Task<List<(JobPosting posting, bool success, Exception ex)>> Process(IList<JobPosting> postings)
        {
            if (!postings.Any())
            {
                _logger.LogInformation("No new postings discovered");
                return new List<(JobPosting posting, bool success, Exception ex)>();
            }

            var results = new List<(JobPosting posting, bool success, Exception ex)>();

            foreach (var posting in postings)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"{posting.Text} ({posting.Id} / {posting.CreatedAtDTime:g})");
                    Console.WriteLine($"{posting.DepartmentTeamGroup}");
                    Console.WriteLine($"{posting.HostedUrl}");

                    _logger.LogInformation($"Processed posting with id {posting.Id}");

                    results.Add((posting, true, null));
                }
                catch (Exception ex)
                {
                    results.Add((posting, false, ex));
                }
            }

            return results;
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

    public class OpeningsTrackerScriptConfig
    {
        public IList<string> DepartmentBlacklist { get; set; } = new List<string>();
        public IList<string> TeamBlacklist { get; set; } = new List<string>();
    }
}