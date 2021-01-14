// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Watcher
{
    public record DotNetWatchOptions(
        bool SuppressHandlingStaticContentFiles,
        bool SuppressMSBuildIncrementalism)
    {
        public static DotNetWatchOptions Default { get; } = new DotNetWatchOptions
        (
            SuppressHandlingStaticContentFiles: GetSuppressedValue("DOTNET_WATCH_SUPPRESS_STATIC_FILE_HANDLING"),
            SuppressMSBuildIncrementalism: GetSuppressedValue("DOTNET_WATCH_SUPPRESS_MSBUILD_INCREMENTALISM")
        );

        private static bool GetSuppressedValue(string key)
        {
            var envValue = Environment.GetEnvironmentVariable(key);
            return envValue == "1" || envValue == "true";
        }
    }
}
