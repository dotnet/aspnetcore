// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures
{
    public class Config
    {
        public List<string> BuildIdAllowList { get; set; }
        public EmailConfig Email { get; set; }
        public TeamCityConfig TeamCity { get; set; }
        public GitHubConfig GitHub { get; set; }
    }
}
