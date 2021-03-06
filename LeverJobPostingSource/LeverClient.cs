﻿using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core;

namespace OpeningsTracker.JobPostingSources.Lever
{
    public class LeverClient
    {
        private readonly HttpClient _client;

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public LeverClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<LeverPosting>> GetPostings(string postingUri, CancellationToken token)
        {
            var response = await _client.GetAsync($"{postingUri}", token);
            response.EnsureSuccessStatusCode();

            return await JsonSerializer.DeserializeAsync<List<LeverPosting>>(await response.Content.ReadAsStreamAsync(), _serializerOptions, token);
        }
    }

    public static class Extensions
    {
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
}