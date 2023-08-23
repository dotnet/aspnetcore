// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.UI.Services;

/// <summary>
/// The default <see cref="IEmailSender"/> that does nothing in <see cref="SendEmailAsync(string, string, string)"/>.
/// It is used to detect that the <see cref="IEmailSender" /> has been customized. If not, Identity UI provides a development
/// experience where the email confirmation link is rendered by the UI immediately rather than sent via an email.
/// </summary>
public sealed class NoOpEmailSender : IEmailSender
{
    /// <summary>
    /// This method does nothing other return <see cref="Task.CompletedTask"/>. It should be replaced by a custom implementation
    /// in production.
    /// </summary>
    public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
}
