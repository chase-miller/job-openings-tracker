using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker
{
    class OpeningsTrackerScript
    {
        private readonly LeverClient _leverClient;
        private readonly Database _database;
        private readonly OpeningsTrackerScriptConfig _config;

        public OpeningsTrackerScript(LeverClient leverClient, Database database, OpeningsTrackerScriptConfig config)
        {
            _leverClient = leverClient;
            _database = database;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var databaseFile = await _database.GetDatabaseFile(cancellationToken);

            var newItems = (await _leverClient.GetPostings(cancellationToken))
                .ExceptAlreadyProcessed(databaseFile.AlreadyProcessedIds)
                .ExceptBlacklistedDepartments(_config.DepartmentBlacklist)
                .ExceptBlacklistedTeams(_config.TeamBlacklist);

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
        public static IEnumerable<LeverPosting> ExceptAlreadyProcessed(this IEnumerable<LeverPosting> postings, IEnumerable<string> alreadyProcessedIds) =>
            postings.ExceptBy(
                alreadyProcessedIds, 
                posting => posting.Id,
                alreadyProcessedId => alreadyProcessedId
            );

        public static IEnumerable<LeverPosting> ExceptBlacklistedDepartments(this IEnumerable<LeverPosting> postings, IEnumerable<string> blacklistedDepartments) =>
            postings.ExceptBy(
                blacklistedDepartments,
                posting => posting.Categories.Department,
                blacklistedDepartment => blacklistedDepartment
            );

        public static IEnumerable<LeverPosting> ExceptBlacklistedTeams(this IEnumerable<LeverPosting> postings, IEnumerable<string> blacklistedTeams) =>
            postings.ExceptBy(
                blacklistedTeams,
                posting => posting.Categories.Team,
                blacklistedTeam => blacklistedTeam
            );
    }

    class OpeningsTrackerScriptConfig
    {
        public IList<string> DepartmentBlacklist { get; set; } = new List<string>();
        public IList<string> TeamBlacklist { get; set; } = new List<string>();
    }
}