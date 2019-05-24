// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GetDefaultStaticWebAssetsBasePath : Task
    {
        [Required]
        public string BasePath { get; set; }

        [Output]
        public string SafeBasePath { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(BasePath))
            {
                Log.LogError($"Base path '{BasePath ?? "(null)"}' must contain non-whitespace characters.");
                return !Log.HasLoggedErrors;
            }

            var safeBasePath = BasePath
                .Replace(" ", "")
                .Replace(".", "")
                .ToLowerInvariant();

            if (safeBasePath == "")
            {
                Log.LogError($"Base path '{BasePath}' must contain non '.' characters.");
                return !Log.HasLoggedErrors;
            }
            SafeBasePath = safeBasePath;

            return !Log.HasLoggedErrors;
        }
    }
}