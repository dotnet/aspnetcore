// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace GitHubProvider
{
    public class GitHubLabel
    {
        public int Id { get; set; }
        public Uri Url { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public bool Default { get; set; }
    }
}