// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Identity.ExternalClaims.Services;

namespace Identity.ExternalClaims.Services;

public static class EmailSenderExtensions
{
    public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
    {
        return emailSender.SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(link)}'>clicking here</a>.");
    }

    public static Task SendResetPasswordAsync(this IEmailSender emailSender, string email, string callbackUrl)
    {
        return emailSender.SendEmailAsync(email, "Reset Password",
            $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
    }
}
