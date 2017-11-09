// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Versioning;

namespace RepoTasks.Utilities
{
    public class VersionUtilities
    {
        public static string GetTimestampFreeVersion(string packageVersion)
        {
            var version = new NuGetVersion(packageVersion);
            var updatedVersion = new NuGetVersion(version.Version, GetTimestampFreeReleaseLabel(version.Release));
            return  updatedVersion.ToNormalizedString();
        }

        public static string GetTimestampFreeReleaseLabel(string releaseLabel)
        {
            if (releaseLabel.StartsWith("rtm-", StringComparison.OrdinalIgnoreCase))
            {
                // E.g. change version 2.5.0-rtm-123123 to 2.5.0.
                releaseLabel = string.Empty;
            }
            else
            {
                var timeStampFreeVersion = Environment.GetEnvironmentVariable("TIMESTAMP_FREE_VERSION");
                if (string.IsNullOrEmpty(timeStampFreeVersion))
                {
                    timeStampFreeVersion = "final";
                }

                if (!timeStampFreeVersion.StartsWith("-"))
                {
                    timeStampFreeVersion = "-" + timeStampFreeVersion;
                }

                // E.g. change version 2.5.0-rc2-123123 to 2.5.0-rc2-final.
                var index = releaseLabel.LastIndexOf('-');
                if (index != -1)
                {
                    releaseLabel = releaseLabel.Substring(0, index) + timeStampFreeVersion;
                }
            }

            return releaseLabel;
        }
    }
}