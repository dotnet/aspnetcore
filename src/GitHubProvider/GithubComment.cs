// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace GitHubProvider
{
    public class GithubComment
    {
        public int Id { get; set; }
        public Uri Url { get; set; }
        public Uri HtmlUrl { get; set; }
        public string Body { get; set; }
        public GitHubUser User { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}