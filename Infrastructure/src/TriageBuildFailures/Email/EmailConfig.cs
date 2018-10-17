// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.Email
{
    public class EmailConfig
    {
        public string QuietEmail { get; set; }
        public string EngineeringAlias { get; set; }
        public string BuildBuddyEmail { get; set; }
        public string FromEmail { get; set; }
        public SmtpConfig SmtpConfig { get; set; }
    }
}
