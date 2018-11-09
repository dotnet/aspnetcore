// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.GitHub
{
    public class GitHubAreaConfig
    {
        /// <summary>
        /// The label name for this product area. For example, 'area-mvc'.
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// A comma-separated list of GitHub owner names to assign and @mention for issues in this area.
        /// </summary>
        public string OwnerNames { get; set; }
    }
}
