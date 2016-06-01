using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;

namespace PushCoherence
{
    public static class PackagePublisher
    {
        public static async Task PublishToFeedAsync(IEnumerable<string> packagePaths, string feed, string apiKey)
        {
            using (var semaphore = new SemaphoreSlim(4))
            {
                var sourceRepository = CreateSourceRepository(feed);

                var metadataResource = await sourceRepository.GetResourceAsync<MetadataResource>();
                var packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
                var tasks = packagePaths.Select(async packagePath =>
                {
                    await semaphore.WaitAsync(TimeSpan.FromMinutes(3));
                    try
                    {
                        var packageIdentity = Program.GetPackageIdentity(packagePath);

                        if (await IsAlreadyUploadedAsync(metadataResource, packageIdentity))
                        {
                            Console.WriteLine($"Skipping {packageIdentity} since it is already published.");
                            return;
                        }

                        var attempt = 0;
                        while (attempt < 10)
                        {
                            attempt++;
                            Console.WriteLine($"Attempting to publish package {packageIdentity} (Attempt: {attempt})");
                            try
                            {
                                await packageUpdateResource.Push(
                                    packagePath,
                                    symbolsSource: null,
                                    timeoutInSecond: 60,
                                    disableBuffering: false,
                                    getApiKey: _ => apiKey,
                                    log: NullLogger.Instance);
                                Console.WriteLine($"Done publishing package {packageIdentity}");
                                return;
                            }
                            catch (Exception ex) when (attempt < 9)
                            {
                                Console.Error.WriteLine($"Attempt {(10 - attempt)} failed.{Environment.NewLine}{ex}{Environment.NewLine}Retrying...");
                                await Task.Delay(TimeSpan.FromSeconds(attempt));
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
        }

        private static SourceRepository CreateSourceRepository(string feed)
        {
            var settings = Settings.LoadDefaultSettings(
                Directory.GetCurrentDirectory(),
                configFileName: null,
                machineWideSettings: null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(
                new PackageSourceProvider(settings),
                FactoryExtensionsV2.GetCoreV3(Repository.Provider));

            feed = feed.TrimEnd('/') + "/api/v3/index.json";
            return sourceRepositoryProvider.CreateRepository(new PackageSource(feed));
        }

        private static async Task<bool> IsAlreadyUploadedAsync(MetadataResource resource, PackageIdentity packageId)
        {
            if (resource == null)
            {
                // If we couldn't locate the v3 feed, republish the packages
                return false;
            }

            try
            {
                return await resource.Exists(packageId, NullLogger.Instance, default(CancellationToken));
            }
            catch (Exception ex)
            {
                // If we can't read feed info, republish the packages
                var exceptionMessage = (ex?.InnerException ?? ex.GetBaseException()).Message;
                Console.WriteLine($"Failed to read package existence {Environment.NewLine}{exceptionMessage}.");
                return false;
            }
        }
    }
}