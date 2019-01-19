// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.DefaultUI.WebSite
{
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
}
