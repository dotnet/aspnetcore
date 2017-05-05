using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace E2ETests
{
    internal class Store : IDisposable
    {
        public const string MusicStoreAspNetCoreStoreFeed = "MUSICSTORE_ASPNETCORE_STORE_FEED";
        private readonly ILogger _logger;
        private string _storeDir;
        private string _tempDir;

        public Store(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Store>();
        }

        public string CreateStore(bool createInDefaultLocation)
        {
            var storeParentDir = GetStoreParentDirectory(createInDefaultLocation);

            InstallStore(storeParentDir);

            _storeDir = Path.Combine(storeParentDir, "store");

            return _storeDir;
        }

        public void Dispose()
        {
            if (string.IsNullOrEmpty(_storeDir))
            {
                return;
            }

            if (Helpers.PreservePublishedApplicationForDebugging)
            {
                _logger.LogInformation("Skipping deleting the store as it has been disabled");
            }
            else
            {
                _logger.LogInformation("Deleting the store...");

                //RetryHelper.RetryOperation(
                //        () => Directory.Delete(_storeDir, recursive: true),
                //        e => _logger.LogError($"Failed to delete directory : {e.Message}"),
                //        retryCount: 3,
                //        retryDelayMilliseconds: 100);

                RetryHelper.RetryOperation(
                        () => Directory.Delete(_tempDir, recursive: true),
                        e => _logger.LogError($"Failed to delete directory : {e.Message}"),
                        retryCount: 3,
                        retryDelayMilliseconds: 100);
            }
        }

        public static bool IsEnabled()
        {
            var storeFeed = Environment.GetEnvironmentVariable(MusicStoreAspNetCoreStoreFeed);
            return !string.IsNullOrEmpty(storeFeed);
        }

        private void InstallStore(string storeParentDir)
        {
            var packageId = "Build.RS";
            var storeFeed = Environment.GetEnvironmentVariable(MusicStoreAspNetCoreStoreFeed);
            if (string.IsNullOrEmpty(storeFeed))
            {
                _logger.LogError("The feed for the store package was not provided." +
                    $"Set the environment variable '{MusicStoreAspNetCoreStoreFeed}' and try again.");

                throw new InvalidOperationException(
                    $"The environment variable '{MusicStoreAspNetCoreStoreFeed}' is not defined or is empty.");
            }
            else
            {
                _logger.LogInformation($"Using the feed {storeFeed} for the store package");
            }

            // Get the version information from the attribute which is typically the same as the nuget package version
            // Example:
            // [assembly: AssemblyInformationalVersion("2.0.0-preview1-24847")]
            var aspnetCoreHttpAssembly = typeof(Microsoft.AspNetCore.Http.FormCollection).Assembly;
            var obj = aspnetCoreHttpAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), inherit: false).FirstOrDefault();
            var assemblyInformationVersionAttribute = obj as AssemblyInformationalVersionAttribute;
            if (assemblyInformationVersionAttribute == null)
            {
                throw new InvalidOperationException($"Could not find {nameof(assemblyInformationVersionAttribute)} from the assembly {aspnetCoreHttpAssembly.FullName}");
            }

            _logger.LogInformation($"Downloading package with id {packageId} and version {assemblyInformationVersionAttribute.InformationalVersion} from feed {storeFeed}");

            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var sourceRepository = Repository.Factory.GetCoreV2(new PackageSource(storeFeed));
            var downloadResource = sourceRepository.GetResource<DownloadResource>();
            var result = downloadResource.GetDownloadResourceResultAsync(
                new PackageIdentity(packageId, NuGetVersion.Parse(assemblyInformationVersionAttribute.InformationalVersion)),
                new PackageDownloadContext(
                    new SourceCacheContext() { NoCache = true, DirectDownload = true },
                    _tempDir,
                    directDownload: true),
                null,
                NuGet.Common.NullLogger.Instance,
                CancellationToken.None)
                .Result;

            if (result.Status != DownloadResourceResultStatus.Available)
            {
                _logger.LogError($"Failed to download the package. Status: {result.Status}");
                throw new InvalidOperationException("Unable to download the store package");
            }

            var zipFile = Path.Combine(_tempDir, "Build.RS.zip");
            using (var targetStream = File.Create(zipFile))
            {
                using (result.PackageStream)
                {
                    result.PackageStream.CopyTo(targetStream);
                }
            }

            _logger.LogInformation($"Package downloaded and saved as zip file at {zipFile}");

            var zipFileExtracted = Path.Combine(_tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipFile, zipFileExtracted);

            _logger.LogInformation($"Package extracted at {zipFileExtracted}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string fileNameWithExtension = null;
                foreach (var file in new DirectoryInfo(zipFileExtracted).GetFiles())
                {
                    if (file.Name.StartsWith($"{packageId}.winx64"))
                    {
                        using (var zipArchive = ZipFile.Open(file.FullName, ZipArchiveMode.Read))
                        {
                            var mvcCoreDllEntry = zipArchive.Entries
                                .Where(entry => string.Equals(entry.Name, "Microsoft.AspNetCore.Mvc.Core.dll", StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault();
                            if (mvcCoreDllEntry != null && mvcCoreDllEntry.FullName.Contains(assemblyInformationVersionAttribute.InformationalVersion))
                            {
                                fileNameWithExtension = file.Name;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(fileNameWithExtension))
                {
                    throw new InvalidOperationException(
                        $"Could not find a store zip file with version {assemblyInformationVersionAttribute.InformationalVersion}");
                }

                var storeZipFile = Path.Combine(zipFileExtracted, fileNameWithExtension);
                ZipFile.ExtractToDirectory(storeZipFile, storeParentDir);
                _logger.LogInformation($"Extracted the store zip file '{storeZipFile}' to '{storeParentDir}'");
            }
            else
            {
                string packageIdPrefix;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    packageIdPrefix = $"{packageId}.linux";
                }
                else
                {
                    packageIdPrefix = $"{packageId}.osx";
                }

                string fileNameWithExtension = null;
                foreach (var file in new DirectoryInfo(zipFileExtracted).GetFiles())
                {
                    if (file.Name.StartsWith(packageIdPrefix)
                        && !string.Equals($"{packageIdPrefix}.tar.gz", file.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        fileNameWithExtension = file.FullName;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(fileNameWithExtension))
                {
                    throw new InvalidOperationException(
                        $"Could not find a store zip file with version {assemblyInformationVersionAttribute.InformationalVersion}");
                }

                Directory.CreateDirectory(storeParentDir);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = "tar",
                    Arguments = $"xvzf {fileNameWithExtension}",
                    WorkingDirectory = storeParentDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };
                var tarProcess = new Process() { StartInfo = startInfo };
                tarProcess.EnableRaisingEvents = true;
                tarProcess.StartAndCaptureOutAndErrToLogger("tar", _logger);

                if (tarProcess.HasExited && tarProcess.ExitCode != 0)
                {
                    var message = $"Error occurred while extracting the file '{fileNameWithExtension}' in working directory '{storeParentDir}'";
                    _logger.LogError(message);
                    throw new InvalidOperationException(message);
                }
            }
        }

        private string GetStoreParentDirectory(bool createInDefaultLocation)
        {
            string storeParentDir;
            if (createInDefaultLocation)
            {
                // On Windows: ..\.dotnet\x64\dotnet.exe
                // On Linux  : ../.dotnet/dotnet
                var dotnetDir = new FileInfo(DotNetMuxer.MuxerPath).Directory.FullName;
                storeParentDir = dotnetDir;
            }
            else
            {
                storeParentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            }
            return storeParentDir;
        }
    }
}
