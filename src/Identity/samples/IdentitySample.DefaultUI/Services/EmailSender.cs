// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity.UI.Services;

namespace IdentitySample.DefaultUI.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, subject);
        _logger.LogInformation("Email body: {Body}", htmlMessage);
        return Task.CompletedTask;
    }
}
