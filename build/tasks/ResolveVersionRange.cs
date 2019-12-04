// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class ResolveVersionRange : Task
    {
        [Required]
        [Output]
        public ITaskItem[] Items { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public string RangeType { get; set; }

        // MSBuild doesn't allow binding to enums directly.
        private enum VersionRangeType
        {
            Minimum, // [1.1.1, )
            MajorMinor, // [1.1.1, 1.2.0)
        }

        public override bool Execute()
        {
            if (!Enum.TryParse<VersionRangeType>(RangeType, out var rangeType))
            {
                Log.LogError("Unexpected value {0} for RangeType", RangeType);
                return false;
            }

            var versionRange = GetVersionRange(rangeType, Version);

            foreach (var item in Items)
            {
                item.SetMetadata("_OriginalVersion", Version);
                item.SetMetadata("Version", versionRange);
            }

            return !Log.HasLoggedErrors;
        }

        private string GetVersionRange(VersionRangeType rangeType, string packageVersion)
        {
            switch (rangeType)
            {
                case VersionRangeType.MajorMinor:
                    if (!NuGetVersion.TryParse(packageVersion, out var nugetVersion))
                    {
                        Log.LogError("Invalid NuGet version '{0}'", packageVersion);
                        return null;
                    }
                    return $"[{packageVersion}, {nugetVersion.Major}.{nugetVersion.Minor + 1}.0)";
                case VersionRangeType.Minimum:
                    return packageVersion;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
