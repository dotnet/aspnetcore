// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IdentitySample.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
