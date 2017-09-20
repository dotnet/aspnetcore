// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Build;
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Task = System.Threading.Tasks.Task;

namespace RepoTasks
{
    public class DownloadNuGetPackages : Microsoft.Build.Utilities.Task, ICancelableTask
    {
        private static readonly Task<bool> FalseTask = Task.FromResult(false);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public string DestinationFolder { get; set; }

        [Output]
        public ITaskItem[] Files { get; set; }

        public void Cancel() => _cts.Cancel();

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> ExecuteAsync()
        {
            DestinationFolder = DestinationFolder.Replace('\\', '/');

            var requests = new Dictionary<string, List<PackageIdentity>>(StringComparer.OrdinalIgnoreCase);
            var files = new List<ITaskItem>();
            var downloadCount = 0;
            foreach (var item in Packages)
            {
                var id = item.ItemSpec;
                var rawVersion = item.GetMetadata("Version");
                if (!NuGetVersion.TryParse(rawVersion, out var version))
                {
                    Log.LogError($"Package '{id}' has an invalid 'Version' metadata value: '{rawVersion}'.");
                    return false;
                }

                var source = item.GetMetadata("Source");
                if (string.IsNullOrEmpty(source))
                {
                    Log.LogError($"Package '{id}' is missing the 'Source' metadata value.");
                    return false;
                }

                if (!requests.TryGetValue(source, out var packages))
                {
                    packages = requests[source] = new List<PackageIdentity>();
                }

                var request = new PackageIdentity(id, version);
                var dest = GetExpectedOutputPath(request);
                files.Add(new TaskItem(dest));
                if (File.Exists(dest))
                {
                    Log.LogMessage($"Skipping {request.Id} {request.Version}. Already exists in '{dest}'");
                    continue;
                }
                else
                {
                    downloadCount++;
                    packages.Add(request);
                }
            }

            Files = files.ToArray();

            if (downloadCount == 0)
            {
                Log.LogMessage("All packages are downloaded.");
                return true;
            }

            Directory.CreateDirectory(DestinationFolder);
            var logger = new MSBuildLogger(Log);
            var timer = Stopwatch.StartNew();

            logger.LogMinimal($"Downloading {downloadCount} package(s)");

            using (var cacheContext = new SourceCacheContext())
            {
                var defaultSettings = Settings.LoadDefaultSettings(root: null, configFileName: null, machineWideSettings: null);
                var sourceProvider = new CachingSourceProvider(new PackageSourceProvider(defaultSettings));
                var tasks = new List<Task<bool>>();

                foreach (var feed in requests)
                {
                    var repo = sourceProvider.CreateRepository(new PackageSource(feed.Key));
                    tasks.Add(DownloadPackagesAsync(repo, feed.Value, cacheContext, logger, _cts.Token));
                }

                var all = Task.WhenAll(tasks);
                var wait = TimeSpan.FromSeconds(Math.Max(downloadCount * 5, 30));
                var delay = Task.Delay(wait);

                var finished = await Task.WhenAny(all, delay);
                if (ReferenceEquals(delay, finished))
                {
                    Log.LogError($"Timed out after {wait.TotalSeconds}s");
                    Cancel();
                    return false;
                }

                if (!tasks.All(a => a.Result))
                {
                    Log.LogError("Failed to download all packages");
                    return false;
                }

                timer.Stop();
                logger.LogMinimal($"Finished downloading {downloadCount} package(s) in {timer.ElapsedMilliseconds}ms");
                return true;
            }
        }

        private async Task<bool> DownloadPackagesAsync(
            SourceRepository repo,
            IEnumerable<PackageIdentity> requests,
            SourceCacheContext cacheContext,
            NuGet.Common.ILogger logger,
            CancellationToken cancellationToken)
        {
            var remoteLibraryProvider = new SourceRepositoryDependencyProvider(repo, logger, cacheContext, ignoreFailedSources: false, ignoreWarning: false);
            var downloads = new List<Task<bool>>();
            var metadataResource = await repo.GetResourceAsync<MetadataResource>();

            foreach (var request in requests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (metadataResource != null && !await metadataResource.Exists(request, logger, cancellationToken))
                {
                    logger.LogError($"Package {request.Id} {request.Version} is not available on '{repo}'");
                    downloads.Add(FalseTask);
                    continue;
                }

                var download = DownloadPackageAsync(cacheContext, logger, remoteLibraryProvider, request, cancellationToken);
                downloads.Add(download);
            }

            await Task.WhenAll(downloads);
            return downloads.All(d => d.Result);
        }

        private async Task<bool> DownloadPackageAsync(SourceCacheContext cacheContext,
            NuGet.Common.ILogger logger,
            SourceRepositoryDependencyProvider remoteLibraryProvider,
            PackageIdentity request,
            CancellationToken cancellationToken)
        {
            var dest = GetExpectedOutputPath(request);
            logger.LogInformation($"Downloading {request.Id} {request.Version} to '{dest}'");

            using (var packageDependency = await remoteLibraryProvider.GetPackageDownloaderAsync(request, cacheContext, logger, cancellationToken))
            {
                if (!await packageDependency.CopyNupkgFileToAsync(dest, cancellationToken))
                {
                    logger.LogError($"Could not download {request.Id} {request.Version} from {remoteLibraryProvider.Source}");
                    return false;
                }
            }

            return true;
        }

        private string GetExpectedOutputPath(PackageIdentity request)
        {
            return Path.Combine(DestinationFolder, $"{request.Id.ToLowerInvariant()}.{request.Version.ToNormalizedString()}.nupkg");
        }
    }
}
