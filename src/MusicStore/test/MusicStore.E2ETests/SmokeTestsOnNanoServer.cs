using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // These tests publish(on the machine where these tests on run) the MusicStore app to a local folder first
    // and then copy the published output to the target NanoServer and launch them.
    public class SmokeTestsOnNanoServerUsingStandaloneRuntime
    {
        private readonly SmokeTestsOnNanoServer _smokeTestsOnNanoServer;
        private readonly RemoteDeploymentConfig _remoteDeploymentConfig;

        public SmokeTestsOnNanoServerUsingStandaloneRuntime(ITestOutputHelper output)
        {
            _remoteDeploymentConfig = RemoteDeploymentConfigHelper.GetConfiguration();
            _smokeTestsOnNanoServer = new SmokeTestsOnNanoServer(output, _remoteDeploymentConfig);
        }

        [ConditionalTheory, Trait("E2ETests", "NanoServer")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [SkipIfEnvironmentVariableNotEnabled("RUN_TESTS_ON_NANO")]
        [InlineData(ServerType.Kestrel, 5000, ApplicationType.Standalone)]
        [InlineData(ServerType.HttpSys, 5000, ApplicationType.Standalone)]
        [InlineData(ServerType.IIS, 8080, ApplicationType.Standalone)]
        public async Task Test(ServerType serverType, int portToListen, ApplicationType applicationType)
        {
            var applicationBaseUrl = $"http://{_remoteDeploymentConfig.ServerName}:{portToListen}/";
            await _smokeTestsOnNanoServer.RunTestsAsync(serverType, applicationBaseUrl, applicationType);
        }
    }

    // Tests here test portable app scenario, so we copy the dotnet runtime onto the
    // target server's file share and after setting up a remote session to the server, we update the PATH environment
    // to have the path to this copied dotnet runtime folder in the share.
    // The dotnet runtime is copied only once for all the tests in this class.
    public class SmokeTestsOnNanoServerUsingSharedRuntime
        : IClassFixture<SmokeTestsOnNanoServerUsingSharedRuntime.DotnetRuntimeSetupTestFixture>
    {
        private readonly SmokeTestsOnNanoServer _smokeTestsOnNanoServer;
        private readonly RemoteDeploymentConfig _remoteDeploymentConfig;

        public SmokeTestsOnNanoServerUsingSharedRuntime(
            DotnetRuntimeSetupTestFixture dotnetRuntimeSetupTestFixture, ITestOutputHelper output)
        {
            _remoteDeploymentConfig = RemoteDeploymentConfigHelper.GetConfiguration();
            _remoteDeploymentConfig.DotnetRuntimePathOnShare = dotnetRuntimeSetupTestFixture.DotnetRuntimePathOnShare;
            _smokeTestsOnNanoServer = new SmokeTestsOnNanoServer(output, _remoteDeploymentConfig);
        }

        [ConditionalTheory, Trait("E2Etests", "NanoServer")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [SkipIfEnvironmentVariableNotEnabled("RUN_TESTS_ON_NANO")]
        [InlineData(ServerType.Kestrel, 5000, ApplicationType.Portable)]
        [InlineData(ServerType.HttpSys, 5000, ApplicationType.Portable)]
        [InlineData(ServerType.IIS, 8080, ApplicationType.Portable)]
        public async Task Test(ServerType serverType, int portToListen, ApplicationType applicationType)
        {
            var applicationBaseUrl = $"http://{_remoteDeploymentConfig.ServerName}:{portToListen}/";
            await _smokeTestsOnNanoServer.RunTestsAsync(serverType, applicationBaseUrl, applicationType);
        }

        // Copies dotnet runtime to the target server's file share.
        public class DotnetRuntimeSetupTestFixture : IDisposable
        {
            private bool copiedDotnetRuntime;

            public DotnetRuntimeSetupTestFixture()
            {
                var runNanoServerTests = Environment.GetEnvironmentVariable("RUN_TESTS_ON_NANO");
                if (string.IsNullOrWhiteSpace(runNanoServerTests)
                    || string.IsNullOrEmpty(runNanoServerTests)
                    || runNanoServerTests.ToLower() == "false")
                {
                    return;
                }

                RemoteDeploymentConfig = RemoteDeploymentConfigHelper.GetConfiguration();

                DotnetRuntimePathOnShare = Path.Combine(RemoteDeploymentConfig.FileSharePath, "dotnet_" + Guid.NewGuid());

                // Prefer copying the zip file to fileshare and extracting on file share over copying the extracted
                // dotnet runtime folder from source to file share as the size could be significantly huge.
                if (!string.IsNullOrEmpty(RemoteDeploymentConfig.DotnetRuntimeZipFilePath))
                {
                    if (!File.Exists(RemoteDeploymentConfig.DotnetRuntimeZipFilePath))
                    {
                        throw new InvalidOperationException(
                           $"Expected dotnet runtime zip file at '{RemoteDeploymentConfig.DotnetRuntimeFolderPath}', but didn't find one.");
                    }

                    ZippedDotnetRuntimePathOnShare = Path.Combine(
                        RemoteDeploymentConfig.FileSharePath,
                        Path.GetFileName(RemoteDeploymentConfig.DotnetRuntimeZipFilePath));

                    if (!File.Exists(ZippedDotnetRuntimePathOnShare))
                    {
                        File.Copy(RemoteDeploymentConfig.DotnetRuntimeZipFilePath, ZippedDotnetRuntimePathOnShare, overwrite: true);
                        Console.WriteLine($"Copied the local dotnet zip folder '{RemoteDeploymentConfig.DotnetRuntimeZipFilePath}' " +
                            $"to the file share path '{RemoteDeploymentConfig.FileSharePath}'");
                    }

                    if (Directory.Exists(DotnetRuntimePathOnShare))
                    {
                        Directory.Delete(DotnetRuntimePathOnShare, recursive: true);
                    }

                    ZipFile.ExtractToDirectory(ZippedDotnetRuntimePathOnShare, DotnetRuntimePathOnShare);
                    copiedDotnetRuntime = true;
                    Console.WriteLine($"Extracted dotnet runtime to folder '{DotnetRuntimePathOnShare}'");
                }
                else if (!string.IsNullOrEmpty(RemoteDeploymentConfig.DotnetRuntimeFolderPath))
                {
                    if (!Directory.Exists(RemoteDeploymentConfig.DotnetRuntimeFolderPath))
                    {
                        throw new InvalidOperationException(
                            $"Expected dotnet runtime folder at '{RemoteDeploymentConfig.DotnetRuntimeFolderPath}', but didn't find one.");
                    }

                    Console.WriteLine($"Copying dotnet runtime folder from '{RemoteDeploymentConfig.DotnetRuntimeFolderPath}' to '{DotnetRuntimePathOnShare}'.");
                    Console.WriteLine("This could take some time.");

                    DirectoryCopy(RemoteDeploymentConfig.DotnetRuntimeFolderPath, DotnetRuntimePathOnShare, copySubDirs: true);
                    copiedDotnetRuntime = true;
                }
                else
                {
                    throw new InvalidOperationException("Dotnet runtime is required to be copied for testing portable apps scenario. " +
                        $"Either supply '{nameof(RemoteDeploymentConfig.DotnetRuntimeFolderPath)}' containing the unzipped dotnet runtime content or " +
                        $"supply the dotnet runtime zip file path via '{nameof(RemoteDeploymentConfig.DotnetRuntimeZipFilePath)}'.");
                }
            }

            public RemoteDeploymentConfig RemoteDeploymentConfig { get; }

            public string ZippedDotnetRuntimePathOnShare { get; }

            public string DotnetRuntimePathOnShare { get; }

            private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
            {
                var dir = new DirectoryInfo(sourceDirName);

                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: "
                        + sourceDirName);
                }

                var dirs = dir.GetDirectories();
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    var temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, false);
                }

                if (copySubDirs)
                {
                    foreach (var subdir in dirs)
                    {
                        var temppath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                    }
                }
            }

            public void Dispose()
            {
                if (!copiedDotnetRuntime)
                {
                    return;
                }

                // In case the source is provided as a folder itself, then we wouldn't have the zip file to begin with.
                if (!string.IsNullOrEmpty(ZippedDotnetRuntimePathOnShare))
                {
                    try
                    {
                        Console.WriteLine($"Deleting the dotnet runtime zip file '{ZippedDotnetRuntimePathOnShare}'");
                        File.Delete(ZippedDotnetRuntimePathOnShare);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete the dotnet runtime zip file '{ZippedDotnetRuntimePathOnShare}'. Exception: "
                            + ex.ToString());
                    }
                }

                try
                {
                    Console.WriteLine($"Deleting the dotnet runtime folder '{DotnetRuntimePathOnShare}'");
                    Directory.Delete(DotnetRuntimePathOnShare, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete the dotnet runtime folder '{DotnetRuntimePathOnShare}'. Exception: "
                        + ex.ToString());
                }
            }
        }
    }

    class SmokeTestsOnNanoServer : LoggedTest
    {
        private readonly RemoteDeploymentConfig _remoteDeploymentConfig;

        public SmokeTestsOnNanoServer(ITestOutputHelper output, RemoteDeploymentConfig config) : base(output)
        {
            _remoteDeploymentConfig = config;
        }

        public async Task RunTestsAsync(
            ServerType serverType,
            string applicationBaseUrl,
            ApplicationType applicationType)
        {
            var testName = $"SmokeTestsOnNanoServer_{serverType}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger(nameof(SmokeTestsOnNanoServerUsingStandaloneRuntime));
                var deploymentParameters = new RemoteWindowsDeploymentParameters(
                    Helpers.GetApplicationPath(),
                    _remoteDeploymentConfig.DotnetRuntimePathOnShare,
                    serverType,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64,
                    _remoteDeploymentConfig.FileSharePath,
                    _remoteDeploymentConfig.ServerName,
                    _remoteDeploymentConfig.AccountName,
                    _remoteDeploymentConfig.AccountPassword)
                {
                    TargetFramework = Tfm.NetCoreApp50,
                    ApplicationBaseUriHint = applicationBaseUrl,
                    ApplicationType = applicationType
                };

                deploymentParameters.EnvironmentVariables.Add(
                    new KeyValuePair<string, string>("ASPNETCORE_ENVIRONMENT", "SocialTesting"));

                using (var deployer = new RemoteWindowsDeployer(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    await SmokeTests.RunTestsAsync(deploymentResult, logger);
                }
            }
        }
    }

    static class RemoteDeploymentConfigHelper
    {
        private static RemoteDeploymentConfig _remoteDeploymentConfig;

        public static RemoteDeploymentConfig GetConfiguration()
        {
            if (_remoteDeploymentConfig == null)
            {
                var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("remoteDeploymentConfig.json")
                        .AddUserSecrets("MusicStore.E2ETests")
                        .AddEnvironmentVariables()
                        .Build();

                _remoteDeploymentConfig = new RemoteDeploymentConfig();
                configuration.GetSection("NanoServer").Bind(_remoteDeploymentConfig);
            }

            return _remoteDeploymentConfig;
        }
    }
}
