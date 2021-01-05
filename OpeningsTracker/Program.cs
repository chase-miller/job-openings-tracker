using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;

namespace OpeningsTracker
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logger =>
                    logger
                        .SetMinimumLevel(LogLevel.Error)
                )
                // .ConfigureHostConfiguration(builder => 
                //     builder
                //         .AddEnvironmentVariables()
                //         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                // )
                .ConfigureServices((hostContext, services) =>
                    services
                        .AddHostedService<OpeningsTrackerScript>()
                        .AddHttpClient()
                        .AddTransient<Database>(sp => new Database(sp.GetService<IConfiguration>().GetValue<string>("openingsTrackerDatabaseFile")))
                        .AddTransient<LeverClient>((sp) =>
                        {
                            var leverClientConfig = sp.GetService<IConfiguration>().GetSection("leverClientConfig").Get<LeverClientConfig>();
                            
                            var leverClient = new LeverClient(
                                sp.GetService<IHttpClientFactory>().CreateClient($"{typeof(LeverClient)}"),
                                leverClientConfig
                            );

                            return leverClient;
                        })
                );
    }

    class OpeningsTrackerScript : IHostedService
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
            var newItems = await GetNewItems();
            foreach (var newItem in newItems)
            {
                Console.WriteLine();
                Console.WriteLine($"{newItem.Text} ({newItem.Id} / {newItem.CreatedAtDTime:g})");
                Console.WriteLine($"{newItem.Categories.Commitment}; {newItem.Categories.Department}; {newItem.Categories.Location}; {newItem.Categories.Team}");
                // Console.WriteLine($"{newItem.DescriptionPlain}");
                Console.WriteLine($"{newItem.HostedUrl}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        private async Task<IEnumerable<LeverPosting>> GetNewItems()
        {
            var alreadyProcessedIds = await _database.GetAlreadyProcessedIds();
            var items = await _leverClient.GetPostings();

            return
                from item in items
                join alreadyProcessedId in alreadyProcessedIds on item.Id equals alreadyProcessedId into gj
                from subItem in gj.DefaultIfEmpty()
                where subItem == null
                select item;
        }
    }

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

        public async Task<IList<string>> GetAlreadyProcessedIds()
        {
            if (!File.Exists(_fileLocation))
                return await WriteDatabase(new List<string>());

            await using var openStream = File.OpenRead(_fileLocation);
            return await JsonSerializer.DeserializeAsync<List<string>>(openStream);
        }

        public async Task<IList<string>> WriteDatabase(List<string> contents)
        {
            using var writer = File.CreateText(_fileLocation);

            var jsonString = JsonSerializer.Serialize(contents, options);

            await writer.WriteAsync(jsonString);

            return contents;
        }
    }

    class LeverClient
    {
        private readonly HttpClient _client;
        private readonly LeverClientConfig _config;

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public LeverClient(HttpClient client, LeverClientConfig config)
        {
            _client = client;
            _config = config;
        }

        public async Task<List<LeverPosting>> GetPostings()
        {
            var response = await _client.GetAsync($"{_config.PostingsBaseUri}/olo");
            response.EnsureSuccessStatusCode();

            return await JsonSerializer.DeserializeAsync<List<LeverPosting>>(await response.Content.ReadAsStreamAsync(), _serializerOptions);
        }
    }

    class LeverPosting
    {
        public string Id { get; set; }
        public string HostedUrl { get; set; }
        public string DescriptionPlain { get; set; }
        public long? CreatedAt { get; set; }
        
        public DateTime? CreatedAtDTime => CreatedAt.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt.Value).DateTime : (DateTime?)null;
        public string Text { get; set; }
        public LeverCategory Categories { get; set; }
    }

    class LeverCategory
    {
        public string Commitment { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
        public string Team { get; set; }
    }

    class LeverClientConfig
    {
        public string PostingsBaseUri { get; set; }
    }
}
