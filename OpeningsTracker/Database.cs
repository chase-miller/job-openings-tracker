using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker
{
    class Database
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

        public async Task<DatabaseFile> GetDatabaseFile(CancellationToken token)
        {
            if (!File.Exists(_fileLocation))
                return await WriteDatabase(new DatabaseFile());

            await using var openStream = File.OpenRead(_fileLocation);
            return await JsonSerializer.DeserializeAsync<DatabaseFile>(openStream, null, token);
        }

        public async Task<DatabaseFile> WriteDatabase(DatabaseFile fileContents)
        {
            using var writer = File.CreateText(_fileLocation);

            var jsonString = JsonSerializer.Serialize(fileContents, options);

            await writer.WriteAsync(jsonString);

            return fileContents;
        }

        public async Task MarkPostingAsProcessed(DatabaseFile databaseFile, IEnumerable<LeverPosting> processedPostings)
        {
            databaseFile.AlreadyProcessedIds.AddRange(processedPostings.Select(p => p.Id));
            await WriteDatabase(databaseFile);
        }
    }

    class DatabaseFile
    {
        public List<string> AlreadyProcessedIds { get; set; } = new List<string>();
    }
}