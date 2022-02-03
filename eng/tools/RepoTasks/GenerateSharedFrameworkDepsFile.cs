// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;

namespace RepoTasks;

public class GenerateSharedFrameworkDepsFile : Microsoft.Build.Utilities.Task
{
    [Required]
    public string DepsFilePath { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    [Required]
    public string FrameworkName { get; set; }

    [Required]
    public string FrameworkVersion { get; set; }

    [Required]
    public ITaskItem[] References { get; set; }

    [Required]
    public string RuntimeIdentifier { get; set; }

    [Required]
    public string RuntimePackageName { get; set; }

    [Required]
    public string PlatformManifestOutputPath { get; set; }

    public override bool Execute()
    {
        ExecuteCore();

        return !Log.HasLoggedErrors;
    }

    private void ExecuteCore()
    {
        var target = new TargetInfo(TargetFramework, RuntimeIdentifier, string.Empty, isPortable: false);
        var runtimeFiles = new List<RuntimeFile>();
        var nativeFiles = new List<RuntimeFile>();
        var resourceAssemblies = new List<ResourceAssembly>();
        var platformManifest = new List<string>();

        foreach (var reference in References)
        {
            var filePath = reference.ItemSpec;
            var fileName = Path.GetFileName(filePath);
            var fileVersion = FileUtilities.GetFileVersion(filePath)?.ToString() ?? string.Empty;
            var assemblyVersion = FileUtilities.GetAssemblyName(filePath)?.Version;
            if (assemblyVersion == null)
            {
                var nativeFile = new RuntimeFile(fileName, null, fileVersion);
                nativeFiles.Add(nativeFile);
                platformManifest.Add($"{fileName}|{FrameworkName}||{fileVersion}");
            }
            else
            {
                var runtimeFile = new RuntimeFile(fileName,
                    fileVersion: fileVersion,
                    assemblyVersion: assemblyVersion.ToString());
                runtimeFiles.Add(runtimeFile);
                platformManifest.Add($"{fileName}|{FrameworkName}|{assemblyVersion}|{fileVersion}");
            }
        }

        var runtimeLibrary = new RuntimeLibrary("package",
           RuntimePackageName,
           FrameworkVersion,
           hash: string.Empty,
           runtimeAssemblyGroups: new[] { new RuntimeAssetGroup(string.Empty, runtimeFiles) },
           nativeLibraryGroups: new[] { new RuntimeAssetGroup(string.Empty, nativeFiles) },
           Enumerable.Empty<ResourceAssembly>(),
           Array.Empty<Dependency>(),
           hashPath: null,
           path: $"{RuntimePackageName.ToLowerInvariant()}/{FrameworkVersion}",
           serviceable: true);

        var context = new DependencyContext(target,
            CompilationOptions.Default,
            Enumerable.Empty<CompilationLibrary>(),
            new[] { runtimeLibrary },
            Enumerable.Empty<RuntimeFallbacks>());

        Directory.CreateDirectory(Path.GetDirectoryName(DepsFilePath));
        Directory.CreateDirectory(Path.GetDirectoryName(PlatformManifestOutputPath));

        File.WriteAllText(
            PlatformManifestOutputPath,
            string.Join("\n", platformManifest.OrderBy(n => n)),
            Encoding.UTF8);

        try
        {
            using (var depsStream = File.Create(DepsFilePath))
            {
                new DependencyContextWriter().Write(context, depsStream);
            }
        }
        catch (Exception ex)
        {
            // If there is a problem, ensure we don't write a partially complete version to disk.
            if (File.Exists(DepsFilePath))
            {
                File.Delete(DepsFilePath);
            }
            Log.LogErrorFromException(ex);
        }
    }
}
