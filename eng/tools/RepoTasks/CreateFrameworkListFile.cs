// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RepoTasks
{
    public class CreateFrameworkListFile : Task
    {
        /// <summary>
        /// Files to extract basic information from and include in the list.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// A list of assembly names with Profile classifications. A Profile="%(Profile)" attribute
        /// is set in the framework list for the matching Files item if %(Profile) contains text.
        ///
        /// If *any* FileProfiles are passed:
        ///
        ///   *Every* file that ends up listed in the framework list must have a matching
        ///   FileProfile, even if %(Profile) is not set.
        ///
        ///   Additionally, every FileProfile must find exactly one File.
        ///
        /// This task fails if the conditions aren't met. This ensures the classification doesn't
        /// become out of date when the list of files changes.
        ///
        /// %(Identity): Assembly name (including ".dll").
        /// %(Profile): List of profiles that apply, semicolon-delimited.
        /// </summary>
        public ITaskItem[] FileProfiles { get; set; }

        [Required]
        public string TargetFile { get; set; }

        public string[] TargetFilePrefixes { get; set; }

        /// <summary>
        /// Extra attributes to place on the root node.
        ///
        /// %(Identity): Attribute name.
        /// %(Value): Attribute value.
        /// </summary>
        public ITaskItem[] RootAttributes { get; set; }

        public override bool Execute()
        {
            // while (!Debugger.IsAttached)
            // {

            // }
            XAttribute[] rootAttributes = RootAttributes
                ?.Select(item => new XAttribute(item.ItemSpec, item.GetMetadata("Value")))
                .ToArray();

            var frameworkManifest = new XElement("FileList", rootAttributes);

            Dictionary<string, string> fileProfileLookup = FileProfiles
                ?.ToDictionary(
                    item => item.ItemSpec,
                    item => item.GetMetadata("Profile"),
                    StringComparer.OrdinalIgnoreCase);

            var usedFileProfiles = new HashSet<string>();

            foreach (var f in Files
                .Where(IsTargetPathIncluded)
                .Select(item => new
                {
                    Item = item,
                    Filename = Path.GetFileName(item.ItemSpec),
                    TargetPath = item.GetMetadata("TargetPath"),
                    AssemblyName = FileUtilities.GetAssemblyName(item.ItemSpec),
                    FileVersion = FileUtilities.GetFileVersion(item.ItemSpec),
                    IsNative = item.GetMetadata("IsNative") == "true",
                    IsSymbolFile = item.GetMetadata("IsSymbolFile") == "true"
                })
                .Where(f =>
                    !f.IsSymbolFile &&
                    (f.Filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.IsNative))
                .OrderBy(f => f.TargetPath, StringComparer.Ordinal)
                .ThenBy(f => f.Filename, StringComparer.Ordinal))
            {
                var element = new XElement(
                    "File",
                    new XAttribute("Type", f.IsNative ? "Native" : "Managed"),
                    new XAttribute(
                        "Path",
                        Path.Combine(f.TargetPath, f.Filename).Replace('\\', '/')));

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

                if (fileProfileLookup != null)
                {
                    if (fileProfileLookup.TryGetValue(f.Filename, out string profile))
                    {
                        if (!string.IsNullOrEmpty(profile))
                        {
                            element.Add(new XAttribute("Profile", profile));
                        }

                        usedFileProfiles.Add(f.Filename);
                    }
                    else
                    {
                        Log.LogError($"File matches no profile classification: {f.Filename}");
                    }
                }

                frameworkManifest.Add(element);
            }

            foreach (var unused in fileProfileLookup
                ?.Keys.Except(usedFileProfiles).OrderBy(p => p)
                ?? Enumerable.Empty<string>())
            {
                Log.LogError($"Profile classification matches no files: {unused}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
            File.WriteAllText(TargetFile, frameworkManifest.ToString());

            return !Log.HasLoggedErrors;
        }

        private bool IsTargetPathIncluded(ITaskItem item)
        {
            return TargetFilePrefixes
                ?.Any(prefix => item.GetMetadata("TargetPath")?.StartsWith(prefix) == true) ?? true;
        }
    }
}
