using System;
using System.Collections.Generic;
using System.Text;

namespace OpeningsTracker.Notifiers.EmailNotifier
{
    public class EmailNotifierConfig
    {
        public string ToAddress { get; set; }
        public string ToName { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
        public bool RequiresSmtpAuth { get; set; }
    }
}
