// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;

namespace TriageBuildFailures
{
    public class Config
    {
        public EmailConfig Email { get; set; }
        public TeamCityConfig TeamCity { get; set; }
        public GitHubConfig GitHub { get; set; }
    }
}
