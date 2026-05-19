// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity.UI.Services;

namespace Identity.DefaultUI.WebSite;

public class ContosoEmailSender : IEmailSender
{
    public IList<IdentityEmail> SentEmails { get; set; } = new List<IdentityEmail>();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        SentEmails.Add(new IdentityEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }
}
