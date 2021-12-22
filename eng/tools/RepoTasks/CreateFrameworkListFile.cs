// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks;

public class CreateFrameworkListFile : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Files to extract basic information from and include in the list.
    /// </summary>
    [Required]
    public ITaskItem[] Files { get; set; }

    [Required]
    public string TargetFile { get; set; }

    /// <summary>
    /// Extra attributes to place on the root node.
    ///
    /// %(Identity): Attribute name.
    /// %(Value): Attribute value.
    /// </summary>
    public ITaskItem[] RootAttributes { get; set; }

    public override bool Execute()
    {
        XAttribute[] rootAttributes = RootAttributes
            ?.Select(item => new XAttribute(item.ItemSpec, item.GetMetadata("Value")))
            .ToArray();

        var frameworkManifest = new XElement("FileList", rootAttributes);

        var usedFileProfiles = new HashSet<string>();

        foreach (var f in Files
            .Select(item => new
            {
                Item = item,
                Filename = Path.GetFileName(item.ItemSpec),
                AssemblyName = FileUtilities.GetAssemblyName(item.ItemSpec),
                FileVersion = FileUtilities.GetFileVersion(item.ItemSpec),
                IsNative = item.GetMetadata("IsNativeImage") == "true",
                IsSymbolFile = item.GetMetadata("IsSymbolFile") == "true",
                PackagePath = GetPackagePath(item)
            })
            .Where(f =>
                !f.IsSymbolFile &&
                (f.Filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.IsNative))
            .OrderBy(f => f.Filename, StringComparer.Ordinal))
        {
            string path = Path.Combine(f.PackagePath, f.Filename).Replace('\\', '/');
            string type = f.IsNative ? "Native" : "Managed";
            var element = new XElement("File", new XAttribute("Path", path));

            if (path.StartsWith("analyzers/", StringComparison.Ordinal))
            {
                type = "Analyzer";

                if (path.EndsWith(".resources.dll", StringComparison.Ordinal))
                {
                    // omit analyzer resources
                    continue;
                }

                var pathParts = path.Split('/');

                if (pathParts.Length < 3 || !pathParts[1].Equals("dotnet", StringComparison.Ordinal) || pathParts.Length > 5)
                {
                    Log.LogError($"Unexpected analyzer path format {path}.  Expected  'analyzers/dotnet(/roslyn<version>)(/language)/analyzer.dll");
                }

                // Check if we have enough parts for language directory and include it.
                // There could be a roslyn<version> folder before the language folder. Check for it.
                bool hasRoslynVersion = pathParts[2].StartsWith("roslyn", StringComparison.Ordinal);
                int languageLengthCheck = hasRoslynVersion ? 4 : 3;
                int potentialLanguageIndex = hasRoslynVersion ? 3 : 2;
                if (pathParts.Length > languageLengthCheck)
                {
                    element.Add(new XAttribute("Language", pathParts[potentialLanguageIndex]));
                }
            }

            element.Add(new XAttribute("Type", type));

            if (f.AssemblyName != null)
            {
                byte[] publicKeyToken = f.AssemblyName.GetPublicKeyToken();
                string publicKeyTokenHex;

                if (publicKeyToken != null)
                {
                    publicKeyTokenHex = BitConverter.ToString(publicKeyToken)
                        .ToLowerInvariant()
                        .Replace("-", "");
                }
                else
                {
                    Log.LogError($"No public key token found for assembly {f.Item.ItemSpec}");
                    publicKeyTokenHex = "";
                }

                element.Add(
                    new XAttribute("AssemblyName", f.AssemblyName.Name),
                    new XAttribute("PublicKeyToken", publicKeyTokenHex),
                    new XAttribute("AssemblyVersion", f.AssemblyName.Version));
            }
            else if (!f.IsNative)
            {
                // This file isn't managed and isn't native. Leave it off the list.
                continue;
            }

            element.Add(new XAttribute("FileVersion", f.FileVersion));

            frameworkManifest.Add(element);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
        File.WriteAllText(TargetFile, frameworkManifest.ToString());

        return !Log.HasLoggedErrors;
    }
    private static string GetPackagePath(ITaskItem item)
    {
        string packagePath = item.GetMetadata("PackagePath");

        // replicate the logic used by PackTask https://github.com/NuGet/NuGet.Client/blob/f24bad0668193ce21a1db8cabd1ce95ba509c7f0/src/NuGet.Core/NuGet.Build.Tasks.Pack/PackTaskLogic.cs#L644-L647
        string recursiveDir = item.GetMetadata("RecursiveDir");
        recursiveDir = string.IsNullOrEmpty(recursiveDir) ? item.GetMetadata("NuGetRecursiveDir") : recursiveDir;

        return string.IsNullOrEmpty(recursiveDir) ? packagePath :
            Path.Combine(packagePath, recursiveDir);
    }
}
