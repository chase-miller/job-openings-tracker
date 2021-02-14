using System;
using System.Collections.Generic;
using System.Text;
using OpeningsTracker.Core.Models;

namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public static class JobPostingPlaintextEmailBodyExtension
    {
        public static string ToPlaintextEmailBody(this JobPosting jobPosting)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{jobPosting.Text} (created at { jobPosting.CreatedAtDTime:g})");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine(jobPosting.HostedUrl);

            return stringBuilder.ToString();
        }
    }
}
