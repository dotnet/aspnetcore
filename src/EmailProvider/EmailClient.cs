// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Common;
using Microsoft.Extensions.Tools.Internal;
using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EmailProvider
{
    public class EmailClient
    {
        private IReporter _reporter;
        private SmtpClient _smtpClient;

        private const string _from = "raas@ryanbrandenburg.com";

        public EmailClient(IReporter reporter)
        {
            _smtpClient = new SmtpClient();
            _reporter = reporter;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (Static.BeQuite)
            {
                var tempMsg = $"We tried to send an email to {to} about {subject} with {body}";

                _reporter.Output(tempMsg);
                if (!Directory.Exists(to))
                {
                    Directory.CreateDirectory(to);
                }

                using (var streamWriter = File.CreateText(Path.Combine(to, $"{Guid.NewGuid().ToString()}.txt")))
                {
                    streamWriter.Write(tempMsg);
                }
            }
            else
            {
                using (var mailMessage = new MailMessage(_from, to, subject, body))
                {
                    await _smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
    }
}
