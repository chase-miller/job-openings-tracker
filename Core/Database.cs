using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker.Core
{
    public class Database
    {
        private readonly string _fileLocation;

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public Database(string fileLocation)
        {
            _fileLocation = fileLocation ?? throw new ArgumentNullException(nameof(fileLocation));
        }

        public async Task<DatabaseFileV1> GetDatabaseFile(CancellationToken token)
        {
            if (!File.Exists(_fileLocation))
                return await WriteDatabase(new DatabaseFileV1());

            using var openStream = File.OpenRead(_fileLocation);
            return await JsonSerializer.DeserializeAsync<DatabaseFileV1>(openStream, null, token);
        }

        public async Task<DatabaseFileV1> WriteDatabase(DatabaseFileV1 fileV1Contents)
        {
            using var writer = File.CreateText(_fileLocation);

            var jsonString = JsonSerializer.Serialize(fileV1Contents, options);

            await writer.WriteAsync(jsonString);

            return fileV1Contents;
        }

        public async Task MarkPostingAsProcessed(DatabaseFileV1 databaseFileV1, IEnumerable<JobPosting> processedPostings)
        {
            var converted = processedPostings
                .Select(p => new ProcessedPosting
                {
                    Id = p.Id,
                    SourceSystem = p.SourceSystemId
                })
                .ToList();

            databaseFileV1.AlreadyProcessedIds.AddRange(converted);

            await WriteDatabase(databaseFileV1);
        }
    }

    public class DatabaseFileV1
    {
        public int Version { get; set; }
        public List<ProcessedPosting> AlreadyProcessedIds { get; set; } = new List<ProcessedPosting>();

        public DatabaseFileV1()
        {
            Version = 1;
        }
    }

    public class ProcessedPosting
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }
}