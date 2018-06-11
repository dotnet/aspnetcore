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
        private IReporter _reporter;
        private SmtpClient _smtpClient;

        public EmailConfig Config { get; private set; }

        public EmailClient(EmailConfig config, IReporter reporter)
        {
            _smtpClient = new SmtpClient(config.SMTPConfig.Host, config.SMTPConfig.Port);
            _smtpClient.UseDefaultCredentials = false;
            _smtpClient.Credentials = new NetworkCredential(config.SMTPConfig.Login, config.SMTPConfig.Password);
            _smtpClient.EnableSsl = true;
            _reporter = reporter;
            Config = config;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (Constants.BeQuite)
            {
                to = Config.QuiteEmail;
            }

            using (var mailMessage = new MailMessage(Config.FromEmail, to, subject, body))
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}
