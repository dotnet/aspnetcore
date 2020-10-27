// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ValidateStaticWebAssetsUniquePaths : Task
    {
        private const string BasePath = "BasePath";
        private const string RelativePath = "RelativePath";
        private const string TargetPath = "TargetPath";

        [Required]
        public ITaskItem[] StaticWebAssets { get; set; }

        [Required]
        public ITaskItem[] WebRootFiles { get; set; }

        public override bool Execute()
        {
            var assetsByWebRootPaths = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < StaticWebAssets.Length; i++)
            {
                var contentRootDefinition = StaticWebAssets[i];
                if (!EnsureRequiredMetadata(contentRootDefinition, BasePath) ||
                    !EnsureRequiredMetadata(contentRootDefinition, RelativePath))
                {
                    return false;
                }
                else
                {
                    var webRootPath = GetWebRootPath("/wwwroot",
                        contentRootDefinition.GetMetadata(BasePath),
                        contentRootDefinition.GetMetadata(RelativePath));

                    if (assetsByWebRootPaths.TryGetValue(webRootPath, out var existingWebRootPath))
                    {
                        if (!string.Equals(contentRootDefinition.ItemSpec, existingWebRootPath.ItemSpec, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.LogError($"Conflicting assets with the same path '{webRootPath}' for content root paths '{contentRootDefinition.ItemSpec}' and '{existingWebRootPath.ItemSpec}'.");
                            return false;
                        }
                    }
                    else
                    {
                        assetsByWebRootPaths.Add(webRootPath, contentRootDefinition);
                    }
                }
            }

            for (var i = 0; i < WebRootFiles.Length; i++)
            {
                var webRootFile = WebRootFiles[i];
                var relativePath = webRootFile.GetMetadata(TargetPath);
                var webRootFileWebRootPath = GetWebRootPath("", "/", relativePath);
                if (assetsByWebRootPaths.TryGetValue(webRootFileWebRootPath, out var existingAsset))
                {
                    Log.LogError($"The static web asset '{existingAsset.ItemSpec}' has a conflicting web root path '{webRootFileWebRootPath}' with the project file '{webRootFile.ItemSpec}'.");
                    return false;
                }
            }

            return true;
        }

        // Normalizes /base/relative \base\relative\ base\relative and so on to /base/relative
        private string GetWebRootPath(string webRoot, string basePath, string relativePath) => $"{webRoot}/{Path.Combine(basePath, relativePath.TrimStart('.').TrimStart('/')).Replace("\\", "/").Trim('/')}";

        private bool EnsureRequiredMetadata(ITaskItem item, string metadataName)
        {
            var value = item.GetMetadata(metadataName);
            if (string.IsNullOrEmpty(value))
            {
                Log.LogError($"Missing required metadata '{metadataName}' for '{item.ItemSpec}'.");
                return false;
            }

            return true;
        }
    }
}
