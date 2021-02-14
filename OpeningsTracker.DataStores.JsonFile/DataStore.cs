using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.DataStores.JsonFile
{
    public class DataStore : IDataStore
    {
        private readonly string _fileLocation;

        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public DataStore(JsonDataStoreConfig jsonDataStoreConfig)
        {
            var fileLocation = jsonDataStoreConfig.OpeningsTrackerDatabaseFile;
            _fileLocation = fileLocation ?? throw new ArgumentNullException(nameof(fileLocation));
        }

        public async Task<List<ProcessedPosting>> GetProcessedPostings(Func<IQueryable<ProcessedPosting>, IQueryable<ProcessedPosting>> queryFunc, CancellationToken token)
        {
            var dbObj = await GetCreateDatabaseContent(token);

            var query = queryFunc(dbObj.AlreadyProcessedIds.AsQueryable());

            return query.ToList();
        }

        public async Task AddProcessedPostings(IEnumerable<ProcessedPosting> processedPostings, CancellationToken token)
        {
            var entities = await GetCreateDatabaseContent(token);

            entities.AlreadyProcessedIds.AddRange(processedPostings);

            await WriteDatabase(entities);
        }

        private async Task<DatabaseEntitiesV1> WriteDatabase(DatabaseEntitiesV1 entities)
        {
            using var writer = File.CreateText(_fileLocation);

            var jsonString = JsonSerializer.Serialize(entities, _options);

            await writer.WriteAsync(jsonString);

            return entities;
        }

        private async Task<DatabaseEntitiesV1> GetCreateDatabaseContent(CancellationToken token)
        {
            if (!File.Exists(_fileLocation))
                return await WriteDatabase(new DatabaseEntitiesV1());

            using var openStream = File.OpenRead(_fileLocation);
            return await JsonSerializer.DeserializeAsync<DatabaseEntitiesV1>(openStream, null, token);
        }
    }
}