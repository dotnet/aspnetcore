// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Common;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EmailProvider
{
    public class EmailConfig
    {
        public string EngineringAlias { get; set; }
        public string BuildBuddyEmail { get; set; }
        public string FromEmail { get; set; }
    }

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
            if (Constants.BeQuite)
            {
                var tempMsg = $"We tried to send an email to {to} about {subject} with {body}";

                _reporter.Output(tempMsg);
                Directory.CreateDirectory(to);

                using (var streamWriter = File.CreateText(Path.Combine(to, $"{Path.GetRandomFileName()}.txt")))
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
