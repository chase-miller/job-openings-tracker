using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core;

namespace OpeningsTracker.DataStores.JsonFile
{
    public class DataStore : IDataStore
    {
        private readonly string _fileLocation;

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public DataStore(JsonDataStoreConfig jsonDataStoreConfig)
        {
            var fileLocation = jsonDataStoreConfig.OpeningsTrackerDatabaseFile;
            _fileLocation = fileLocation ?? throw new ArgumentNullException(nameof(fileLocation));
        }

        public async Task<DatabaseEntitiesV1> GetDatabaseEntities(CancellationToken token)
        {
            if (!File.Exists(_fileLocation))
                return await WriteDatabase(new DatabaseEntitiesV1());

            using var openStream = File.OpenRead(_fileLocation);
            return await JsonSerializer.DeserializeAsync<DatabaseEntitiesV1>(openStream, null, token);
        }

        public async Task MarkPostingAsProcessed(DatabaseEntitiesV1 entities, IEnumerable<JobPosting> processedPostings)
        {
            var converted = processedPostings
                .Select(p => new ProcessedPosting
                {
                    Id = p.Id,
                    SourceSystem = p.SourceSystemId
                })
                .ToList();

            entities.AlreadyProcessedIds.AddRange(converted);

            await WriteDatabase(entities);
        }

        private async Task<DatabaseEntitiesV1> WriteDatabase(DatabaseEntitiesV1 entities)
        {
            using var writer = File.CreateText(_fileLocation);

            var jsonString = JsonSerializer.Serialize(entities, options);

            await writer.WriteAsync(jsonString);

            return entities;
        }
    }
}