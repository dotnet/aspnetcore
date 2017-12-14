// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Blazor.BuildTools.Core
{
    internal static class Build
    {
        internal static void Execute(string assemblyPath, string webRootPath)
        {
            var clientFileSystem = ClientFileSystem.Instantiate(assemblyPath, webRootPath);
            var distDirPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "dist");
            FileUtil.WriteFileProviderToDisk(clientFileSystem, distDirPath, clean: true);

            // Temporary hack until ClientFileSystem can mount the subdirs in the correct place
            var frameworkPath = Path.Combine(distDirPath, "_framework");
            Directory.CreateDirectory(frameworkPath);
            Directory.Move(Path.Combine(distDirPath, "_bin"), Path.Combine(frameworkPath, "_bin"));
            Directory.Move(Path.Combine(distDirPath, "asmjs"), Path.Combine(frameworkPath, "asmjs"));
            Directory.Move(Path.Combine(distDirPath, "wasm"), Path.Combine(frameworkPath, "wasm"));
            File.Move(Path.Combine(distDirPath, "blazor.js"), Path.Combine(frameworkPath, "blazor.js"));
        }
    }
}
