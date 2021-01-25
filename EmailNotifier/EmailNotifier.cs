using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using OpeningsTracker.Core;

namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public class EmailNotifier : IJobPostingNotifier
    {
        private readonly ILogger<EmailNotifier> _logger;
        private readonly EmailNotifierConfig _config;

        public EmailNotifier(ILogger<EmailNotifier> logger, EmailNotifierConfig config)
        {
            _logger = logger;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IEnumerable<(JobPosting posting, bool success, Exception ex)>> Notify(IEnumerable<JobPosting> postings, CancellationToken cancellationToken)
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

                    await SendEmail(posting, cancellationToken);

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

        private async Task SendEmail(JobPosting posting, CancellationToken cancellationToken)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));
            message.To.Add(new MailboxAddress(_config.ToName, _config.ToAddress));
            message.Subject = $"New Job Posting - {posting.ShortDescription}";

            // TODO - make this html...and what you'd actually want.
            message.Body = new TextPart("plain")
            {
                Text = posting.Text
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(_config.SmtpHost, 587, false, cancellationToken);

            // Note: only needed if the SMTP server requires authentication
            if (_config.RequiresSmtpAuth)
                await client.AuthenticateAsync(_config.SmtpUser, _config.SmtpPassword, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
