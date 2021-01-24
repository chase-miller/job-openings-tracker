using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpeningsTracker.Core;

namespace LeverJobPostingSource
{
    public class LeverService : IJobPostingSource
    {
        private readonly LeverClient _client;
        private readonly LeverConfig _config;

        public LeverService(LeverClient client, LeverConfig config)
        {
            _client = client;
            _config = config;
        }

        public async Task<List<JobPosting>> GetPostings(CancellationToken token)
        {
            var postings = (await _client.GetPostings(token))
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

        public string PostingSourceId => "Lever_00cb6681d5934702aaf613bf59f32880";
    }
}
