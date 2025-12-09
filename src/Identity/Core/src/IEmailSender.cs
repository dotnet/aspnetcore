// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.UI.Services;

/// <summary>
/// This API supports the ASP.NET Core Identity infrastructure and is not intended to be used as a general purpose
/// email abstraction. It should be implemented by the application so the Identity infrastructure can send confirmation and password reset emails.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// This API supports the ASP.NET Core Identity infrastructure and is not intended to be used as a general purpose
    /// email abstraction. It should be implemented by the application so the Identity infrastructure can send confirmation and password reset emails.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="htmlMessage">The body of the email which may contain HTML tags. Do not double encode this.</param>
    /// <returns></returns>
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
