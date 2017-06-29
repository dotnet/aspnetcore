using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace E2ETests
{
    internal class Store : IDisposable
    {
        private readonly ILogger _logger;
        private string _storeParentDir;
        private string _storeDir;

        public Store(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Store>();
        }

        public string CreateStore()
        {
            _storeParentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            InstallStore(_storeParentDir);

            _storeDir = Path.Combine(_storeParentDir, "store");

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
                _logger.LogInformation($"Deleting the store directory {_storeParentDir}...");

                RetryHelper.RetryOperation(
                        () => Directory.Delete(_storeParentDir, recursive: true),
                        e => _logger.LogError($"Failed to delete directory : {e.Message}"),
                        retryCount: 3,
                        retryDelayMilliseconds: 100);
            }
        }

        public static bool IsEnabled()
        {
            var storeFeed = Environment.GetEnvironmentVariable("RUN_RUNTIME_STORE_TESTS");
            return !string.IsNullOrEmpty(storeFeed);
        }

        private void InstallStore(string storeParentDir)
        {
            var packageId = "Build.RS";

            var runtimeStoreLibrary = DependencyContext.Default.RuntimeLibraries
                .Where(library => string.Equals("Build.RS", library.Name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (runtimeStoreLibrary == null)
            {
                throw new InvalidOperationException($"Could not find the package with id '{packageId}' in {nameof(DependencyContext)}.");
            }

            var runtimeStoreVersion = runtimeStoreLibrary.Version;
            var restoredRuntimeStorePackageDir = Path.Combine(GetNugetPackagesRoot(), runtimeStoreLibrary.Path);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string fileNameWithExtension = null;
                foreach (var file in new DirectoryInfo(restoredRuntimeStorePackageDir).GetFiles())
                {
                    if (file.Name.StartsWith($"{packageId}.winx64"))
                    {
                        using (var zipArchive = ZipFile.Open(file.FullName, ZipArchiveMode.Read))
                        {
                            var mvcCoreDllEntry = zipArchive.Entries
                                .Where(entry => string.Equals(entry.Name, "Microsoft.AspNetCore.Mvc.Core.dll", StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault();
                            if (mvcCoreDllEntry != null && mvcCoreDllEntry.FullName.Contains(runtimeStoreVersion))
                            {
                                fileNameWithExtension = file.Name;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(fileNameWithExtension))
                {
                    throw new InvalidOperationException($"Could not find a store zip file with version {runtimeStoreVersion}");
                }

                var storeZipFile = Path.Combine(restoredRuntimeStorePackageDir, fileNameWithExtension);
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
                foreach (var file in new DirectoryInfo(restoredRuntimeStorePackageDir).GetFiles())
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
                        $"Could not find a store zip file with version {runtimeStoreVersion}");
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

        private string GetNugetPackagesRoot()
        {
            var packageDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(packageDirectory))
            {
                return packageDirectory;
            }

            string basePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }

            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            return Path.Combine(basePath, ".nuget", "packages");
        }
    }
}
