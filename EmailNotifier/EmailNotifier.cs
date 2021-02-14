using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using OpeningsTracker.Core;
using OpeningsTracker.Core.Models;
using Polly;
using Polly.Retry;

namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public class EmailNotifier : IJobPostingNotifier
    {
        private readonly ILogger<EmailNotifier> _logger;
        private readonly EmailNotifierConfig _config;

        private static readonly Random Jitterer = new Random();
        private static readonly AsyncRetryPolicy RetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(6,    // exponential back-off plus some jitter
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(400, retryAttempt))
                                + TimeSpan.FromMilliseconds(Jitterer.Next(0, 100))
            );

        public string NotifierId => "EmailNotifier";

        public EmailNotifier(ILogger<EmailNotifier> logger, EmailNotifierConfig config)
        {
            _logger = logger;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IEnumerable<(JobPosting posting, bool success, Exception ex)>> Notify(IEnumerable<JobPosting> postings, CancellationToken cancellationToken)
        {
            var results = new List<(JobPosting posting, bool success, Exception ex)>();

            using var client = new SmtpClient();

            await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort ?? 587, _config.UseSsl, cancellationToken);

            // Note: only needed if the SMTP server requires authentication
            if (_config.RequiresSmtpAuth)
                await client.AuthenticateAsync(_config.SmtpUser, _config.SmtpPassword, cancellationToken);

            foreach (var posting in postings)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"{posting.Text} ({posting.Id} / {posting.CreatedAtDTime:g})");
                    Console.WriteLine($"{posting.DepartmentTeamGroup}");
                    Console.WriteLine($"{posting.HostedUrl}");

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));
                    message.To.Add(new MailboxAddress(_config.ToName, _config.ToAddress));
                    message.Subject = $"New Job Posting - {posting.ShortDescription}";

                    message.Body = new TextPart("plain")
                    {
                        Text = posting.ToPlaintextEmailBody()
                    };

                    await RetryPolicy.ExecuteAsync(
                        ct => client.SendAsync(message, ct),
                        cancellationToken
                    );

                    _logger.LogInformation($"Processed posting with id {posting.Id}");

                    results.Add((posting, true, null));
                }
                catch (Exception ex)
                {
                    results.Add((posting, false, ex));
                }
            }

            await client.DisconnectAsync(true, cancellationToken);

            return results;
        }
    }
}
