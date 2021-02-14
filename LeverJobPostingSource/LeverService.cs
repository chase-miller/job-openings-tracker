using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.JobPostingSources.Lever
{
    public class LeverService : IJobPostingSource
    {
        private readonly LeverClient _client;
        private readonly LeverConfig _config;

        public LeverService(LeverClient client, LeverConfig config)
        {
            _client = client;
            _config = config;

            if (!config.PostingsUris.Any())
                throw new ArgumentOutOfRangeException(nameof(LeverConfig.PostingsUris), $"At least one {nameof(LeverConfig.PostingsUris)} must be provided");
        }

        public async Task<List<JobPosting>> GetPostings(CancellationToken token)
        {
            var postings = (await GetPostingsAcrossCompanies(token))
                .SelectMany(p => p)
                .ExceptBlacklistedDepartments(_config.DepartmentBlacklist)
                .ExceptBlacklistedTeams(_config.TeamBlacklist);

            return postings
                .Select(p => new JobPosting
                {
                    Id = p.Id,
                    SourceSystemId = PostingSourceId,
                    DepartmentTeamGroup = p.Categories.Department,
                    CreatedAtDTime = p.CreatedAtDTime,
                    HostedUrl = p.HostedUrl,
                    Text = p.Text
                })
                .ToList();
        }

        private async Task<List<List<LeverPosting>>> GetPostingsAcrossCompanies(CancellationToken token)
        {
            var postings = new List<List<LeverPosting>>();

            foreach (var companyUri in _config.PostingsUris)
            {
                var results = await _client.GetPostings(companyUri, token);
                postings.Add(results);
            }

            return postings;
        }

        public string PostingSourceId => "LeverSource";
    }
}
