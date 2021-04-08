// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ShadowCopyTests : IISFunctionalTestBase
    {
        public ShadowCopyTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        public async Task ShadowCopyDoesNotLockFiles()
        {
            using var directory = TempDirectory.Create();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.HandlerSettings["experimentalEnableShadowCopy"] = "true";
            deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("Wow!");

            Assert.True(response.IsSuccessStatusCode);
            var directoryInfo = new DirectoryInfo(deploymentResult.ContentRoot);

            // Verify that we can delete all files in the content root (nothing locked)
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                fileInfo.Delete();
            }

            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                dirInfo.Delete(recursive: true);
            }
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        public async Task ShadowCopyRelativeInSameDirectoryWorks()
        {
            var directoryName = Path.GetRandomFileName();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.HandlerSettings["experimentalEnableShadowCopy"] = "true";
            deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directoryName;

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("Wow!");

            Assert.True(response.IsSuccessStatusCode);
            var directoryInfo = new DirectoryInfo(deploymentResult.ContentRoot);

            // Verify that we can delete all files in the content root (nothing locked)
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                fileInfo.Delete();
            }

            var tempDirectoryPath = Path.Combine(deploymentResult.ContentRoot, directoryName);
            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                if (!tempDirectoryPath.Equals(dirInfo.FullName))
                {
                    dirInfo.Delete(recursive: true);
                }
            }
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        public async Task ShadowCopyRelativeOutsideDirectoryWorks()
        {
            using var directory = TempDirectory.Create();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.HandlerSettings["experimentalEnableShadowCopy"] = "true";
            deploymentParameters.HandlerSettings["shadowCopyDirectory"] = $"..\\{directory.DirectoryInfo.Name}";
            deploymentParameters.ApplicationPath = directory.DirectoryPath;

            var deploymentResult = await DeployAsync(deploymentParameters);
            var response = await deploymentResult.HttpClient.GetAsync("Wow!");

            // Check if directory can be deleted.
            // Can't delete the folder but can delete all content in it.

            Assert.True(response.IsSuccessStatusCode);
            var directoryInfo = new DirectoryInfo(deploymentResult.ContentRoot);

            // Verify that we can delete all files in the content root (nothing locked)
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                fileInfo.Delete();
            }

            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                dirInfo.Delete(recursive: true);
            }

            StopServer();
            deploymentResult.AssertWorkerProcessStop();
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        public async Task ShadowCopySingleFileChangedWorks()
        {
            using var directory = TempDirectory.Create();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.HandlerSettings["experimentalEnableShadowCopy"] = "true";
            deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;

            var deploymentResult = await DeployAsync(deploymentParameters);

            DirectoryCopy(deploymentResult.ContentRoot, directory.DirectoryPath, copySubDirs: true);

            var response = await deploymentResult.HttpClient.GetAsync("Wow!");
            Assert.True(response.IsSuccessStatusCode);
            // Rewrite file
            var dirInfo = new DirectoryInfo(deploymentResult.ContentRoot);

            string dllPath = "";
            foreach (var file in dirInfo.EnumerateFiles())
            {
                if (file.Extension == ".dll")
                {
                    dllPath = file.FullName;
                    break;
                }
            }
            var fileContents = File.ReadAllBytes(dllPath);
            File.WriteAllBytes(dllPath, fileContents);

            deploymentResult.AssertWorkerProcessStop();

            response = await deploymentResult.HttpClient.GetAsync("Wow!");
            Assert.True(response.IsSuccessStatusCode);
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        public async Task ShadowCopyE2EWorksWithFolderPresent()
        {
            using var directory = TempDirectory.Create();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.HandlerSettings["experimentalEnableShadowCopy"] = "true";
            deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;
            var deploymentResult = await DeployAsync(deploymentParameters);

            DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "0"), copySubDirs: true);

            var response = await deploymentResult.HttpClient.GetAsync("Wow!");
            Assert.True(response.IsSuccessStatusCode);

            using var secondTempDir = TempDirectory.Create();

            // copy back and forth to cause file change notifications.
            DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
            DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

            deploymentResult.AssertWorkerProcessStop();
            response = await deploymentResult.HttpClient.GetAsync("Wow!");
            Assert.True(response.IsSuccessStatusCode);
        }

        public class TempDirectory : IDisposable
        {
            public static TempDirectory Create()
            {
                var directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                var directoryInfo = Directory.CreateDirectory(directoryPath);
                return new TempDirectory(directoryInfo);
            }

            public TempDirectory(DirectoryInfo directoryInfo)
            {
                DirectoryInfo = directoryInfo;

                DirectoryPath = directoryInfo.FullName;
            }

            public string DirectoryPath { get; }
            public DirectoryInfo DirectoryInfo { get; }

            public void Dispose()
            {
                DeleteDirectory(DirectoryPath);
            }

            private static void DeleteDirectory(string directoryPath)
            {
                foreach (var subDirectoryPath in Directory.EnumerateDirectories(directoryPath))
                {
                    DeleteDirectory(subDirectoryPath);
                }

                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(directoryPath))
                    {
                        var fileInfo = new FileInfo(filePath)
                        {
                            Attributes = FileAttributes.Normal
                        };
                        fileInfo.Delete();
                    }
                    Directory.Delete(directoryPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"Failed to delete directory {directoryPath}: {e.Message}");
                }
            }
        }

        // copied from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
