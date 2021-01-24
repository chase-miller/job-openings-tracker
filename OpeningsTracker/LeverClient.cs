using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpeningsTracker
{
    class LeverClientConfig
    {
        public string PostingsBaseUri { get; set; }
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

        public async Task<List<LeverPosting>> GetPostings(CancellationToken token)
        {
            var response = await _client.GetAsync($"{_config.PostingsBaseUri}/olo", token);
            response.EnsureSuccessStatusCode();

            return await JsonSerializer.DeserializeAsync<List<LeverPosting>>(await response.Content.ReadAsStreamAsync(), _serializerOptions, token);
        }
    }
}