// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHubProvider
{
    public class GithubIssue
    {
        public Uri Url { get; set; }
        public Uri Repository_Url { get; set; }
        public Uri Labels_Url { get; set; }
        public Uri Comments_Url { get; set; }
        public Uri EventsUrl { get; set; }
        public Uri HtmlUrl { get; set; }
        public int Id { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int Comments { get; set; }
        public IssueState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public IEnumerable<GitHubLabel> Labels { get; set; }

        public string RepositoryName => Repository_Url.AbsolutePath.Split('/').Last();
    }
}