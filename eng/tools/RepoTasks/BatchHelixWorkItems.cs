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
/// Items marked with SkipHelixWorkItemBatching=true or that have pre-commands
/// are excluded from batching and passed through as-is.
/// Batched items use symlinks to assembly publish directories for fast payload creation.
/// </summary>
public class BatchHelixWorkItems : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] WorkItems { get; set; }

    [Required]
    public int MaxBatchSize { get; set; }

    [Required]
    public string OutputDirectory { get; set; }

    [Required]
    public bool IsWindowsQueue { get; set; }

    /// <summary>
    /// Minutes of test execution time to allow per assembly in a batch.
    /// </summary>
    public int TimeoutMinutesPerAssembly { get; set; } = 2;

    /// <summary>
    /// Minutes of fixed overhead to add to each batch timeout (tool installs, result upload, etc.).
    /// </summary>
    public int TimeoutMinutesOverhead { get; set; } = 5;

    /// <summary>
    /// Maximum timeout in minutes for any single batch.
    /// </summary>
    public int MaxTimeoutMinutes { get; set; } = 45;

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
            var skipBatching = workItem.GetMetadata("SkipHelixWorkItemBatching");
            var tfm = workItem.GetMetadata("TargetFrameworkMoniker");
            if (string.IsNullOrWhiteSpace(tfm))
            {
                var separatorIndex = workItem.ItemSpec.LastIndexOf("--", StringComparison.Ordinal);
                tfm = separatorIndex >= 0 ? workItem.ItemSpec.Substring(separatorIndex + 2) : string.Empty;
            }

            var shouldBatch = !string.IsNullOrWhiteSpace(tfm) &&
                string.IsNullOrWhiteSpace(preCommands) &&
                !string.Equals(skipBatching, "true", StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(payloadDirectory);

            if (!shouldBatch)
            {
                // Write a single-entry targets.txt for unbatched items so all items
                // use the same @targets.txt command format.
                if (Directory.Exists(payloadDirectory))
                {
                    var testAssembly = workItem.GetMetadata("TestAssembly");
                    File.WriteAllText(Path.Combine(payloadDirectory, "targets.txt"), testAssembly);
                }

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

                // Copy shared content files (runtests.cmd/sh, NuGet.config, etc.) from the first
                // item's payload root into the batch directory. Only root-level files are copied,
                // not subdirectories (those are assembly-specific and handled via symlinks below).
                var firstPayload = firstItem.GetMetadata("PayloadDirectory");
                foreach (var file in Directory.GetFiles(firstPayload))
                {
                    var destPath = Path.Combine(batchDirectory, Path.GetFileName(file));
                    if (!File.Exists(destPath))
                    {
                        File.Copy(file, destPath);
                    }
                }

                // Create symbolic links from batch subdirectories to assembly publish directories.
                // This is orders of magnitude faster than copying files.
                var targets = new List<string>();
                foreach (var workItem in chunk)
                {
                    var testAssembly = workItem.GetMetadata("TestAssembly");
                    var assemblyName = Path.GetFileNameWithoutExtension(testAssembly);
                    var targetDirectory = Path.Combine(batchDirectory, assemblyName);
                    Directory.CreateSymbolicLink(targetDirectory, workItem.GetMetadata("PayloadDirectory"));

                    // Playwright tests should never be batched — they require special setup.
                    var installPlaywright = workItem.GetMetadata("InstallPlaywright");
                    if (string.Equals(installPlaywright, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError("Work item '{0}' requires Playwright but is being batched. " +
                            "Set SkipHelixWorkItemBatching=true in the project to exclude it.", workItem.ItemSpec);
                        return false;
                    }

                    var relativeSeparator = IsWindowsQueue ? "\\" : "/";
                    targets.Add($"{assemblyName}{relativeSeparator}{testAssembly}");
                }

                var batchTimeout = TimeSpan.FromMinutes(TimeoutMinutesPerAssembly * chunk.Count + TimeoutMinutesOverhead);
                if (batchTimeout > TimeSpan.FromMinutes(MaxTimeoutMinutes))
                {
                    batchTimeout = TimeSpan.FromMinutes(MaxTimeoutMinutes);
                }

                File.WriteAllLines(Path.Combine(batchDirectory, "targets.txt"), targets);

                var batchedWorkItem = new TaskItem($"batch_{batchNumber}--{group.Key}");

                // Build the command from work item metadata rather than parsing command strings.
                var runtimeVersion = firstItem.GetMetadata("RuntimeVersion");
                var queueName = firstItem.GetMetadata("QueueName");
                var arch = firstItem.GetMetadata("TestingArchitecture");
                var quarantined = firstItem.GetMetadata("RunQuarantined");
                var timeoutStr = batchTimeout.ToString("c", CultureInfo.InvariantCulture);
                var script = IsWindowsQueue ? "call runtests.cmd" : "./runtests.sh";
                var command = $"{script} @targets.txt {runtimeVersion} {queueName} {arch} {quarantined} {timeoutStr} false";

                // Copy metadata from the first item, then override batch-specific values.
                foreach (System.Collections.DictionaryEntry metadataEntry in firstItem.CloneCustomMetadata())
                {
                    batchedWorkItem.SetMetadata((string)metadataEntry.Key, (string)metadataEntry.Value);
                }

                batchedWorkItem.SetMetadata("PayloadDirectory", batchDirectory);
                batchedWorkItem.SetMetadata("TestAssembly", "targets.txt");
                batchedWorkItem.SetMetadata("Timeout", timeoutStr);
                batchedWorkItem.SetMetadata("SkipHelixWorkItemBatching", "false");
                batchedWorkItem.SetMetadata("TargetFrameworkMoniker", group.Key);
                batchedWorkItem.SetMetadata("Command", command);

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
}
