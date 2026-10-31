// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.ExternalClaims.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}
