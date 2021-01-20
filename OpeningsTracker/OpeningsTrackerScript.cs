using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker
{
    class OpeningsTrackerScript
    {
        private readonly LeverClient _leverClient;
        private readonly Database _database;

        public OpeningsTrackerScript(LeverClient leverClient, Database database)
        {
            _leverClient = leverClient;
            _database = database;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var databaseFile = await _database.GetDatabaseFile();

            var newItems = (await _leverClient.GetPostings())
                .ExceptAlreadyProcessed(databaseFile.AlreadyProcessedIds);

            var processResults = await Process(newItems);

            var idsToAddToDb = processResults
                .Where(r => r.success)
                .Select(r => r.posting);

            await _database.MarkPostingAsProcessed(databaseFile, idsToAddToDb);
        }

        private async Task<List<(LeverPosting posting, bool success, Exception ex)>> Process(IEnumerable<LeverPosting> postings)
        {
            var results = new List<(LeverPosting posting, bool success, Exception ex)>();

            foreach (var posting in postings)
            {
                Console.WriteLine();
                Console.WriteLine($"{posting.Text} ({posting.Id} / {posting.CreatedAtDTime:g})");
                Console.WriteLine(
                    $"{posting.Categories.Commitment}; {posting.Categories.Department}; {posting.Categories.Location}; {posting.Categories.Team}");
                // Console.WriteLine($"{newItem.DescriptionPlain}");
                Console.WriteLine($"{posting.HostedUrl}");

                results.Add((posting, true, null));
            }

            return results;
        }
    }

    static class LeverPostingExtensions
    {
        public static IEnumerable<LeverPosting> ExceptAlreadyProcessed(this IEnumerable<LeverPosting> postings, List<string> alreadyProcessedIds)
        {
            var asList = postings?.ToList() ?? new List<LeverPosting>();

            var newIds = asList
                .Select(p => p.Id)
                .Except(alreadyProcessedIds);

            return asList.Join(newIds, posting => posting.Id, id => id, (posting, _) => posting);
        }
    }

    class OpeningsTrackerScriptConfig
    {
        public List<string> CategoryBlacklist { get; set; } = new List<string>();
    }
}