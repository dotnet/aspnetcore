// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class CompressBlazorApplicationFiles : Task
    {
        [Required]
        public ITaskItem StaticWebAsset { get; set; }

        public override bool Execute()
        {
            var targetCompressionPath = StaticWebAsset.GetMetadata("TargetCompressionPath");

            Directory.CreateDirectory(Path.GetDirectoryName(targetCompressionPath));

            using var sourceStream = File.OpenRead(StaticWebAsset.GetMetadata("FullPath"));
            using var fileStream = new FileStream(targetCompressionPath, FileMode.Create);
            using var stream = new GZipStream(fileStream, CompressionLevel.Optimal);

            sourceStream.CopyTo(stream);

            return true;
        }
    }
}

