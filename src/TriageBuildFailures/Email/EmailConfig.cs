// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.Email
{
    public class EmailConfig
    {
        public string QuiteEmail { get; set; }
        public string EngineringAlias { get; set; }
        public string BuildBuddyEmail { get; set; }
        public string FromEmail { get; set; }
        public SMTPConfig SMTPConfig { get; set; }
    }
}
