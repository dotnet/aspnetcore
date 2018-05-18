// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Common;
using McMaster.Extensions.CommandLineUtils;
using System.IO;
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
            _smtpClient = new SmtpClient();
            _reporter = reporter;
            Config = config;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (true)
            {
                var tempMsg = $"We tried to send an email to {to} about {subject} with {body}";

                _reporter.Output(tempMsg);
                var folder = Path.Combine("temp", to);
                Directory.CreateDirectory(folder);

                using (var streamWriter = File.CreateText(Path.Combine(folder, $"{Path.GetRandomFileName()}.txt")))
                {
                    streamWriter.Write(tempMsg);
                }
            }
            else
            {
                using (var mailMessage = new MailMessage(Config.FromEmail, to, subject, body))
                {
                    await _smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
    }
}
