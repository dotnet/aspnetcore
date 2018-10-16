// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.TeamCity;
using TriageBuildFailures.VSTS;

namespace TriageBuildFailures
{
    public class Config
    {
        public List<string> BuildIdForbidList { get; set; }
        public EmailConfig Email { get; set; }
        public TeamCityConfig TeamCity { get; set; }
        public GitHubConfig GitHub { get; set; }
        public VSTSConfig VSTS { get; set; }
    }
}
