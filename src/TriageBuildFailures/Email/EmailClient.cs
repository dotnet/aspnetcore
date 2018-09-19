// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Common;
using McMaster.Extensions.CommandLineUtils;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TriageBuildFailures.Email
{
    public class EmailClient
    {
        private readonly IReporter _reporter;
        private SmtpClient _smtpClient;

        public EmailConfig Config { get; private set; }

        public EmailClient(EmailConfig config, IReporter reporter)
        {
            _smtpClient = new SmtpClient(config.SmtpConfig.Host, config.SmtpConfig.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(config.SmtpConfig.Login, config.SmtpConfig.Password),
                EnableSsl = true
            };
            _reporter = reporter;
            Config = config;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (Constants.BeQuiet)
            {
                to = Config.QuietEmail;
            }

            using (var mailMessage = new MailMessage(Config.FromEmail, to, subject, body))
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}
