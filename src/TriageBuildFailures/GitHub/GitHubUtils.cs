// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TriageBuildFailures.GitHub
{
    public static class GitHubUtils
    {
        public static string GetAtMentions(params string[] names)
        {
            return string.Join(", ", names.Select(name => $"@{name}"));
        }

        public static string GetBranchLabel(string branchName)
        {
            return $"Branch:{branchName}";
        }
    }
}
