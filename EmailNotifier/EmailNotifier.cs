using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpeningsTracker.Core;

namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public class EmailNotifier : IJobPostingNotifier
    {
        private readonly ILogger<EmailNotifier> _logger;

        public EmailNotifier(ILogger<EmailNotifier> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<(JobPosting posting, bool success, Exception ex)>> Notify(IEnumerable<JobPosting> postings)
        {
            var results = new List<(JobPosting posting, bool success, Exception ex)>();

            foreach (var posting in postings)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"{posting.Text} ({posting.Id} / {posting.CreatedAtDTime:g})");
                    Console.WriteLine($"{posting.DepartmentTeamGroup}");
                    Console.WriteLine($"{posting.HostedUrl}");

                    _logger.LogInformation($"Processed posting with id {posting.Id}");

                    results.Add((posting, true, null));
                }
                catch (Exception ex)
                {
                    results.Add((posting, false, ex));
                }
            }

            return results;
        }
    }
}
