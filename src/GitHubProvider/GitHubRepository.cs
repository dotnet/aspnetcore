// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace GitHubProvider
{
    public class GitHubRepository
    {
        public int Id { get; set; }
        public GitHubUser Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}