// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks;

/// <summary>
/// Groups eligible Helix work items into batches to reduce per-item overhead.
/// Items with special dependencies (IIS, Playwright, Java, Node, MSSQL) or
/// pre-commands are excluded from batching and passed through as-is.
/// Batched items use symlinks to assembly publish directories for fast payload creation.
/// </summary>
public class BatchHelixWorkItems : Task
{
    [Required]
    public ITaskItem[] WorkItems { get; set; }

    [Required]
    public int MaxBatchSize { get; set; }

    [Required]
    public string OutputDirectory { get; set; }

    [Required]
    public bool IsWindowsQueue { get; set; }

    [Output]
    public ITaskItem[] BatchedWorkItems { get; set; }

    [Output]
    public ITaskItem[] UnbatchedWorkItems { get; set; }

    public override bool Execute()
    {
        var unbatched = new List<ITaskItem>();
        var batched = new List<ITaskItem>();
        var eligibleGroups = new Dictionary<string, List<ITaskItem>>(StringComparer.OrdinalIgnoreCase);
        var batchSize = MaxBatchSize > 0 ? MaxBatchSize : 20;
        var batchRoot = Path.Combine(OutputDirectory, "batched");
        Directory.CreateDirectory(batchRoot);

        foreach (var workItem in WorkItems ?? Array.Empty<ITaskItem>())
        {
            var payloadDirectory = workItem.GetMetadata("PayloadDirectory");
            var preCommands = workItem.GetMetadata("PreCommands");
            var hasSpecialDependencies = workItem.GetMetadata("HasSpecialDependencies");
            var tfm = workItem.GetMetadata("TargetFrameworkMoniker");
            if (string.IsNullOrWhiteSpace(tfm))
            {
                var separatorIndex = workItem.ItemSpec.LastIndexOf("--", StringComparison.Ordinal);
                tfm = separatorIndex >= 0 ? workItem.ItemSpec.Substring(separatorIndex + 2) : string.Empty;
            }

            var shouldBatch = !string.IsNullOrWhiteSpace(tfm) &&
                string.IsNullOrWhiteSpace(preCommands) &&
                !string.Equals(hasSpecialDependencies, "true", StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(payloadDirectory);

            if (!shouldBatch)
            {
                unbatched.Add(workItem);
                continue;
            }

            if (!eligibleGroups.TryGetValue(tfm, out var group))
            {
                group = new List<ITaskItem>();
                eligibleGroups.Add(tfm, group);
            }

            group.Add(workItem);
        }

        var batchNumber = 0;
        foreach (var group in eligibleGroups.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var orderedItems = group.Value.OrderBy(item => item.ItemSpec, StringComparer.OrdinalIgnoreCase).ToList();
            for (var index = 0; index < orderedItems.Count; index += batchSize)
            {
                batchNumber++;
                var chunk = orderedItems.Skip(index).Take(batchSize).ToList();
                var batchDirectory = Path.Combine(batchRoot, $"batch_{batchNumber}");
                if (Directory.Exists(batchDirectory))
                {
                    Directory.Delete(batchDirectory, recursive: true);
                }

                Directory.CreateDirectory(batchDirectory);
                var firstItem = chunk[0];

                // Copy the first item's shared content (runtests.cmd/sh, NuGet.config, etc.) to the batch root.
                // Only copy files directly in the payload root — not subdirectories (those are assembly-specific).
                var firstPayload = firstItem.GetMetadata("PayloadDirectory");
                foreach (var file in Directory.GetFiles(firstPayload))
                {
                    File.Copy(file, Path.Combine(batchDirectory, Path.GetFileName(file)), overwrite: true);
                }

                // Create symbolic links from batch subdirectories to assembly publish directories.
                // This is orders of magnitude faster than copying files.
                var targets = new List<string>();
                foreach (var workItem in chunk)
                {
                    var testAssembly = workItem.GetMetadata("TestAssembly");
                    var assemblyName = Path.GetFileNameWithoutExtension(testAssembly);
                    var targetDirectory = Path.Combine(batchDirectory, assemblyName);
                    CreateDirectorySymlink(targetDirectory, workItem.GetMetadata("PayloadDirectory"));

                    var relativeSeparator = IsWindowsQueue ? "\\" : "/";
                    targets.Add($"{assemblyName}{relativeSeparator}{testAssembly}");
                }

                // Batch timeout: allow 2 minutes per assembly for test execution + 5 minutes
                // for shared overhead (tool installs, result upload). Individual assembly default
                // is 45 minutes, but most small assemblies complete in under 30 seconds.
                var batchTimeout = TimeSpan.FromMinutes(2 * chunk.Count + 5);
                if (batchTimeout > TimeSpan.FromMinutes(45))
                {
                    batchTimeout = TimeSpan.FromMinutes(45);
                }

                File.WriteAllLines(Path.Combine(batchDirectory, "targets.txt"), targets);

                var batchedWorkItem = new TaskItem($"batch_{batchNumber}--{group.Key}");
                foreach (System.Collections.DictionaryEntry metadataEntry in firstItem.CloneCustomMetadata())
                {
                    batchedWorkItem.SetMetadata((string)metadataEntry.Key, (string)metadataEntry.Value);
                }

                // Parse runtime/queue/arch/quarantine args from the first item's original Command.
                // The original command format is:
                //   [call] runtests.cmd|./runtests.sh <target.dll> <runtime> <queue> <arch> <quarantined> <timeout> <playwright>
                var originalCommand = firstItem.GetMetadata("Command") ?? "";
                var commandParts = originalCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var scriptIndex = Array.FindIndex(commandParts, p => p.Contains("runtests."));

                // Args after the script are: target, runtime, queue, arch, quarantined, timeout, playwright
                var runtimeArg = scriptIndex >= 0 && scriptIndex + 2 < commandParts.Length ? commandParts[scriptIndex + 2] : "";
                var queueArg = scriptIndex >= 0 && scriptIndex + 3 < commandParts.Length ? commandParts[scriptIndex + 3] : "";
                var archArg = scriptIndex >= 0 && scriptIndex + 4 < commandParts.Length ? commandParts[scriptIndex + 4] : "x64";
                var quarantinedArg = scriptIndex >= 0 && scriptIndex + 5 < commandParts.Length ? commandParts[scriptIndex + 5] : "false";
                var playwrightArg = scriptIndex >= 0 && scriptIndex + 7 < commandParts.Length ? commandParts[scriptIndex + 7] : "false";

                batchedWorkItem.SetMetadata("PayloadDirectory", batchDirectory);
                batchedWorkItem.SetMetadata("TestAssembly", "targets.txt");
                batchedWorkItem.SetMetadata("Timeout", batchTimeout.ToString("c", CultureInfo.InvariantCulture));
                batchedWorkItem.SetMetadata("HasSpecialDependencies", "false");
                batchedWorkItem.SetMetadata("TargetFrameworkMoniker", group.Key);
                batchedWorkItem.SetMetadata(
                    "Command",
                    IsWindowsQueue
                        ? $"call runtests.cmd @targets.txt {runtimeArg} {queueArg} {archArg} {quarantinedArg} {batchTimeout.ToString("c", CultureInfo.InvariantCulture)} {playwrightArg}"
                        : $"./runtests.sh @targets.txt {runtimeArg} {queueArg} {archArg} {quarantinedArg} {batchTimeout.ToString("c", CultureInfo.InvariantCulture)} {playwrightArg}");

                Log.LogMessage(MessageImportance.High, "Created batched Helix work item {0} with {1} assemblies.", batchedWorkItem.ItemSpec, chunk.Count);
                batched.Add(batchedWorkItem);
            }
        }

        BatchedWorkItems = batched.ToArray();
        UnbatchedWorkItems = unbatched.ToArray();
        Log.LogMessage(
            MessageImportance.High,
            "Helix work item batching retained {0} unbatched items and created {1} batched items.",
            UnbatchedWorkItems.Length,
            BatchedWorkItems.Length);

        return !Log.HasLoggedErrors;
    }

    private static void CreateDirectorySymlink(string linkPath, string targetPath)
    {
        Directory.CreateSymbolicLink(linkPath, targetPath);
    }
}
