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
    /// email abstraction. It should be implemented by the application so the Identity infrastructure can send confirmation and apassword reset emails.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="htmlMessage">The body of the email which may contain HTML tags. Do not double encode this.</param>
    /// <returns></returns>
    Task SendEmailAsync(string email, string subject, string htmlMessage);

    /// <summary>
    /// This API supports the ASP.NET Core Identity infrastructure and is not intended to be used as a general purpose
    /// email abstraction. It should be implemented by the application so the Identity infrastructure can send confirmation emails.
    /// </summary>
    /// <param name="user">The user that is attempting to confirm their email.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="confirmationLink">The link to follow to confirm a user's email. Do not double encode this.</param>
    /// <returns></returns>
    Task SendConfirmationLinkAsync<TUser>(TUser user, string email, string confirmationLink) where TUser : class
    {
        return SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity infrastructure and is not intended to be used as a general purpose
    /// email abstraction. It should be implemented by the application so the Identity infrastructure can send password reset emails.
    /// </summary>
    /// <param name="user">The user that is attempting to reset their password.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="resetLink">The link to follow to reset the user password. Do not double encode this.</param>
    /// <returns></returns>
    Task SendPasswordResetLinkAsync<TUser>(TUser user, string email, string resetLink) where TUser : class
    {
        return SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    /// <summary>
    /// This API supports the ASP.NET Core Identity infrastructure and is not intended to be used as a general purpose
    /// email abstraction. It should be implemented by the application so the Identity infrastructure can send password reset emails.
    /// </summary>
    /// <param name="user">The user that is attempting to reset their password.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="resetCode">The code to use to reset the user password. Do not double encode this.</param>
    /// <returns></returns>
    Task SendPasswordResetCodeAsync<TUser>(TUser user, string email, string resetCode) where TUser : class
    {
        return SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
    }
}
