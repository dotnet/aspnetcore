// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.GitHub
{
    public class GitHubRepoConfig
    {
        public string Name { get; set; }
        public string Manager { get; set; }
        public bool IssuesOnHomeRepo { get; set; }
    }
}
