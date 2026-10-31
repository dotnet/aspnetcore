// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Identity.DefaultUI.WebSite;

public class IdentityEmail
{
    public IdentityEmail(string to, string subject, string body)
    {
        To = to;
        Subject = subject;
        Body = body;
    }

    public string To { get; }
    public string Subject { get; }
    public string Body { get; }
}
