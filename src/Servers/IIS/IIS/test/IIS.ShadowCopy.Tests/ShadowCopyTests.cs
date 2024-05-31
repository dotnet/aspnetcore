// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ShadowCopyTests : IISFunctionalTestBase
{
    public ShadowCopyTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    public async Task ShadowCopyDoesNotLockFiles()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
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
    public async Task ShadowCopyRelativeInSameDirectoryWorks()
    {
        var directoryName = Path.GetRandomFileName();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
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
    public async Task ShadowCopyRelativeOutsideDirectoryWorks()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
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
    public async Task ShadowCopySingleFileChangedWorks()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
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

        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyDeleteFolderDuringShutdownWorks()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);
        var deleteDirPath = Path.Combine(deploymentResult.ContentRoot, "wwwroot/deletethis");
        Directory.CreateDirectory(deleteDirPath);
        File.WriteAllText(Path.Combine(deleteDirPath, "file.dll"), "");

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        AddAppOffline(deploymentResult.ContentRoot);
        await AssertAppOffline(deploymentResult);

        // Delete folder + file after app is shut down
        // Testing specific path on startup where we compare the app directory contents with the shadow copy directory
        Directory.Delete(deleteDirPath, recursive: true);

        RemoveAppOffline(deploymentResult.ContentRoot);

        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyE2EWorksWithFolderPresent()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "0"), copySubDirs: true);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyE2EWorksWithOldFoldersPresent()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        // Start with 1 to exercise the incremental logic
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "1"), copySubDirs: true);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.False(Directory.Exists(Path.Combine(directory.DirectoryPath, "0")), "Expected 0 shadow copy directory to be skipped");

        // Depending on timing, this could result in a shutdown failure, but sometimes it succeeds, handle both situations
        if (!response.IsSuccessStatusCode)
        {
            Assert.True(response.ReasonPhrase == "Application Shutting Down" || response.ReasonPhrase == "Server has been shutdown");
        }

        // This shutdown should trigger a copy to the next highest directory, which will be 2
        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(directory.DirectoryPath, "2")), "Expected 2 shadow copy directory");

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyCleansUpOlderFolders()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        // Start with a bunch of junk
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "1"), copySubDirs: true);
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "3"), copySubDirs: true);
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "10"), copySubDirs: true);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.False(Directory.Exists(Path.Combine(directory.DirectoryPath, "0")), "Expected 0 shadow copy directory to be skipped");

        // Depending on timing, this could result in a shutdown failure, but sometimes it succeeds, handle both situations
        if (!response.IsSuccessStatusCode)
        {
            Assert.True(response.ReasonPhrase == "Application Shutting Down" || response.ReasonPhrase == "Server has been shutdown");
        }

        // This shutdown should trigger a copy to the next highest directory, which will be 11
        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(directory.DirectoryPath, "11")), "Expected 11 shadow copy directory");

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        // Verify old directories were cleaned up
        Assert.False(Directory.Exists(Path.Combine(directory.DirectoryPath, "1")), "Expected 1 shadow copy directory to be deleted");
        Assert.False(Directory.Exists(Path.Combine(directory.DirectoryPath, "3")), "Expected 3 shadow copy directory to be deleted");
    }

    [ConditionalFact]
    public async Task ShadowCopyIgnoresItsOwnDirectoryWithRelativePathSegmentWhenCopying()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = "./ShadowCopy/../ShadowCopy/";
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "0"), copySubDirs: true);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true, ignoreDirectory: "ShadowCopy");

        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "ShadowCopy")));
        // Make sure folder isn't being recursively copied
        Assert.False(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "ShadowCopy", "0", "ShadowCopy")));

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyIgnoresItsOwnDirectoryWhenCopying()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = "./ShadowCopy";
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "0"), copySubDirs: true);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true, ignoreDirectory: "ShadowCopy");

        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "ShadowCopy")));
        // Make sure folder isn't being recursively copied
        Assert.False(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "ShadowCopy", "0", "ShadowCopy")));

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

    // copied from https://learn.microsoft.com/dotnet/standard/io/how-to-copy-directories
    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, string ignoreDirectory = "")
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
                if (string.Equals(subdir.Name, ignoreDirectory))
                {
                    continue;
                }
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }
}
