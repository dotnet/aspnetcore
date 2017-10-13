// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class GetTimestampFreeVersion : Task
    {
        [Required]
        public string TimestampVersion { get; set; }

        [Output]
        public string TimestampFreeVersion { get; set; }

        public override bool Execute()
        {
            TimestampFreeVersion = VersionUtilities.GetTimestampFreeVersion(TimestampVersion);

            return true;
        }
    }
}
