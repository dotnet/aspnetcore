// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Mvc.Testing.Tasks;

/// <summary>
/// Generate a JSON file mapping assemblies to content root paths.
/// </summary>
public class GenerateMvcTestManifestTask : Task
{
    /// <summary>
    /// The path to output the manifest file to.
    /// </summary>
    [Required]
    public string ManifestPath { get; set; }

    /// <summary>
    /// A list of content root paths and assembly names to generate the
    /// manifest from.
    /// </summary>
    [Required]
    public ITaskItem[] Projects { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        using var fileStream = File.Create(ManifestPath);
        var output = new Dictionary<string, string>();
        var manifestDirectory = Path.GetDirectoryName(ManifestPath);

        foreach (var project in Projects)
        {
            var contentRoot = project.GetMetadata("ContentRoot");
            var assemblyName = project.GetMetadata("Identity");
            var relativeContentRoot = GetRelativePath(manifestDirectory, contentRoot);
            output[assemblyName] = relativeContentRoot;
        }

        var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true
        });
        using var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, ownsStream: false, indent: true);
        serializer.WriteObject(writer, output);

        return !Log.HasLoggedErrors;
    }

    private static string GetRelativePath(string? relativeTo, string path)
    {
        if (string.IsNullOrEmpty(relativeTo))
        {
            return path;
        }

        // Ensure the paths are absolute
        string absoluteRelativeTo = Path.GetFullPath(relativeTo);
        string absolutePath = Path.GetFullPath(path);

        // Split the paths into their components
        string[] relativeToParts = absoluteRelativeTo.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
        string[] pathParts = absolutePath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

        // Find the common base path length
        int commonLength = 0;
        while (commonLength < relativeToParts.Length && commonLength < pathParts.Length &&
               string.Equals(relativeToParts[commonLength], pathParts[commonLength], StringComparison.OrdinalIgnoreCase))
        {
            commonLength++;
        }

        // Calculate the number of directories to go up from the relativeTo path
        int upDirectories = relativeToParts.Length - commonLength;

        // Build the relative path
        string relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), new string[upDirectories].Select(_ => "..").ToArray());
        if (commonLength < pathParts.Length)
        {
            if (relativePath.Length > 0)
            {
                relativePath += Path.DirectorySeparatorChar;
            }
            relativePath += string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Skip(commonLength));
        }

        return relativePath;
    }
}
