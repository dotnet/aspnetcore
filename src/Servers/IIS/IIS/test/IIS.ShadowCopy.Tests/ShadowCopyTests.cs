// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[Collection(PublishedSitesCollection.Name)]
public class ShadowCopyTests(PublishedSitesFixture fixture) : IISFunctionalTestBase(fixture)
{

    public bool IsDirectoryEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    [ConditionalFact]
    public async Task ShadowCopy_FailsWithUsefulExceptionMessage_WhenNoPermissionsToShadowCopyDirectory()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.CreateWithNoPermissions(Logger);
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Content.Headers.ContentLength > 0);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access is denied", content, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains(shadowCopyDirectory.DirectoryPath, content, StringComparison.InvariantCultureIgnoreCase);

        shadowCopyDirectory.RestoreAllPermissions();
        // If failed to copy then the shadowCopyDirectory should be empty
        Assert.True(IsDirectoryEmpty(shadowCopyDirectory.DirectoryPath), "Expected shadow copy directory to be empty");
    }

    [ConditionalFact]
    public async Task ShadowCopy_FailsWithUsefulExceptionMessage_WhenNoWritePermissionsToShadowCopyDirectory()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.CreateWithNoWritePermissions(Logger);
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Content.Headers.ContentLength > 0);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(content.Contains("Failed to create destination directory: ", StringComparison.InvariantCultureIgnoreCase) ||
                    content.Contains("Failed to copy file ", StringComparison.InvariantCultureIgnoreCase),
                    "Expected exception message for failure to copy to shadow copy directory");
        Assert.Contains(shadowCopyDirectory.DirectoryPath, content, StringComparison.InvariantCultureIgnoreCase);

        shadowCopyDirectory.RestoreAllPermissions();
        // If failed to copy then the shadowCopyDirectory should be empty
        Assert.True(IsDirectoryEmpty(shadowCopyDirectory.DirectoryPath), "Expected shadow copy directory to be empty");
    }

    [ConditionalFact]
    public async Task ShadowCopyDoesNotLockFiles()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
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
        // Arrange
        var shadowCopyDirectoryName = Path.GetRandomFileName();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectoryName;

        var deploymentResult = await DeployAsync(deploymentParameters);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var directoryInfo = new DirectoryInfo(deploymentResult.ContentRoot);

        // Verify that we can delete all files in the content root (nothing locked)
        foreach (var fileInfo in directoryInfo.GetFiles())
        {
            fileInfo.Delete();
        }

        var tempDirectoryPath = Path.Combine(deploymentResult.ContentRoot, shadowCopyDirectoryName);
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
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = $"..\\{shadowCopyDirectory.DirectoryInfo.Name}";
        deploymentParameters.ApplicationPath = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Check if content root shadowCopyDirectory can be deleted.
        // Can't delete the folder but can delete all content in it.

        // Assert
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
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, shadowCopyDirectory.DirectoryPath, copySubDirs: true);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        // Assert
        Assert.True(response.IsSuccessStatusCode);

        // Arrange
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

        // Act & Assert
        File.WriteAllBytes(dllPath, fileContents);
        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyDetectsSubdirectoryDllChange()
    {
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        // Find a DLL in a subdirectory and touch it to trigger change detection
        var contentRoot = new DirectoryInfo(deploymentResult.ContentRoot);
        string dllPath = null;

        foreach (var subDir in contentRoot.GetDirectories())
        {
            var dll = subDir.GetFiles("*.dll", SearchOption.AllDirectories).FirstOrDefault();
            if (dll is not null)
            {
                dllPath = dll.FullName;
                break;
            }
        }

        Assert.NotNull(dllPath);

        // Rewrite the file to update its timestamp
        var fileContents = File.ReadAllBytes(dllPath);
        File.WriteAllBytes(dllPath, fileContents);

        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyDeleteFolderDuringShutdownWorks()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;

        var deploymentResult = await DeployAsync(deploymentParameters);
        var deleteDirPath = Path.Combine(deploymentResult.ContentRoot, "wwwroot/deletethis");
        Directory.CreateDirectory(deleteDirPath);
        File.WriteAllText(Path.Combine(deleteDirPath, "file.dll"), "");

        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        AddAppOffline(deploymentResult.ContentRoot);
        await AssertAppOffline(deploymentResult);

        // Act

        // Delete folder + file after app is shut down
        // Testing specific path on startup where we compare the app directory contents with the shadow copy directory
        Directory.Delete(deleteDirPath, recursive: true);

        RemoveAppOffline(deploymentResult.ContentRoot);

        // Assert
        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyE2EWorksWithFolderPresent()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "0"), copySubDirs: true);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        // Arrange
        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        // Act & Assert
        await deploymentResult.AssertRecycledAsync();

        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ShadowCopyE2EWorksWithOldFoldersPresent()
    {
        // Arrange
        using var directory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = directory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        // Start with 1 to exercise the incremental logic
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(directory.DirectoryPath, "1"), copySubDirs: true);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        // Arrange
        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        // Act & Assert
        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.False(Directory.Exists(Path.Combine(directory.DirectoryPath, "0")), "Expected 0 shadow copy directory to be skipped");

        // Depending on timing, this could result in a shutdown failure, but sometimes it succeeds, handle both situations
        if (!response.IsSuccessStatusCode)
        {
            Assert.True(response.ReasonPhrase == "Application Shutting Down" || response.ReasonPhrase == "Server has been shutdown");
        }

        // This shutdown should trigger a copy to the next highest directory, which will be 2
        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(directory.DirectoryPath, "2")), "Expected 2 shadow copy directory to exist");

        // Act & Assert
        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);
    }

    [ConditionalFact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/58106")]
    public async Task ShadowCopyCleansUpOlderFolders()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = shadowCopyDirectory.DirectoryPath;
        var deploymentResult = await DeployAsync(deploymentParameters);

        // Start with a bunch of junk
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "1"), copySubDirs: true);
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "3"), copySubDirs: true);
        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "10"), copySubDirs: true);

        // Act & Assert
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        // Arrange
        using var secondTempDir = TempDirectory.Create();

        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true);

        // Act & Assert
        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.False(Directory.Exists(Path.Combine(shadowCopyDirectory.DirectoryPath, "0")), "Expected 0 shadow copy directory to be skipped");

        // Depending on timing, this could result in a shutdown failure, but sometimes it succeeds, handle both situations
        if (!response.IsSuccessStatusCode)
        {
            Assert.True(response.ReasonPhrase == "Application Shutting Down" || response.ReasonPhrase == "Server has been shutdown");
        }

        // This shutdown should trigger a copy to the next highest directory, which will be 11
        await deploymentResult.AssertRecycledAsync();

        Assert.True(Directory.Exists(Path.Combine(shadowCopyDirectory.DirectoryPath, "11")), "Expected 11 shadow copy directory");

        // Act & Assert
        response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        // Verify old directories were cleaned up
        Assert.False(Directory.Exists(Path.Combine(shadowCopyDirectory.DirectoryPath, "1")), "Expected 1 shadow copy directory to be deleted");
        Assert.False(Directory.Exists(Path.Combine(shadowCopyDirectory.DirectoryPath, "3")), "Expected 3 shadow copy directory to be deleted");
    }

    [ConditionalFact]
    public async Task ShadowCopyIgnoresItsOwnDirectoryWithRelativePathSegmentWhenCopying()
    {
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = "./ShadowCopy/../ShadowCopy/";
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "0"), copySubDirs: true);

        // Act & Assert
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");
        Assert.True(response.IsSuccessStatusCode);

        // Arrange
        using var secondTempDir = TempDirectory.Create();

        // Act
        // copy back and forth to cause file change notifications.
        DirectoryCopy(deploymentResult.ContentRoot, secondTempDir.DirectoryPath, copySubDirs: true);
        DirectoryCopy(secondTempDir.DirectoryPath, deploymentResult.ContentRoot, copySubDirs: true, ignoreDirectory: "ShadowCopy");

        // Assert
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
        // Arrange
        using var shadowCopyDirectory = TempDirectory.Create();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["enableShadowCopy"] = "true";
        deploymentParameters.HandlerSettings["shadowCopyDirectory"] = "./ShadowCopy";
        var deploymentResult = await DeployAsync(deploymentParameters);

        DirectoryCopy(deploymentResult.ContentRoot, Path.Combine(shadowCopyDirectory.DirectoryPath, "0"), copySubDirs: true);

        // Act
        var response = await deploymentResult.HttpClient.GetAsync("Wow!");

        // Assert
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

    public static int RunCommand(string command, string arguments, ILogger logger, string logPrefix = null)
    {
        // TODO: Move somewhere else helper class
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var process = new Process { StartInfo = startInfo };
        process.StartAndCaptureOutAndErrToLogger(logPrefix ?? command, logger);
        process.WaitForExit(TimeSpan.FromSeconds(5));
        return process.ExitCode;
    }

    internal static void RemoveAllPermissions(DirectoryInfo directoryInfo)
    {
        var directorySecurity = directoryInfo.GetAccessControl();

        // Take ownership before removing permissions
        var currentUser = WindowsIdentity.GetCurrent().User;
        directorySecurity.SetOwner(currentUser);
        directoryInfo.SetAccessControl(directorySecurity);

        // Remove all existing permissions
        var emptyPermissions = new DirectorySecurity();
        emptyPermissions.SetOwner(currentUser);
        emptyPermissions.SetAccessRuleProtection(true, false); // Disable inheritance, remove all ACEs
        directoryInfo.SetAccessControl(emptyPermissions);
    }

    internal static void RemoveWritePermissions(DirectoryInfo directoryInfo)
    {
        var directorySecurity = directoryInfo.GetAccessControl();
        // Deny Write access for Everyone
        var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

        var denyRule = new FileSystemAccessRule(
            everyone,
            FileSystemRights.Write | FileSystemRights.Delete,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Deny);

        directorySecurity.AddAccessRule(denyRule);
        directoryInfo.SetAccessControl(directorySecurity);
    }

    public class TempDirectoryRestrictedPermissions : TempDirectory
    {
        private bool _hasPermissions;

        protected ILogger Logger { get; init; }

        public TempDirectoryRestrictedPermissions(DirectoryInfo directoryInfo, ILogger logger, bool canRead) : base(directoryInfo)
        {
            Logger = logger;
            if (canRead)
            {
                RemoveWritePermissions(directoryInfo);
            }
            else
            {
                RemoveAllPermissions(directoryInfo);
            }
        }

        public void RestoreAllPermissions()
        {
            if (_hasPermissions)
            {
                return;
            }

            RestoreAllPermissionsInner();

            _hasPermissions = true;
        }

        private void RestoreAllPermissionsInner()
        {
            var res = RunCommand("takeown", $"/F \"{DirectoryPath}\" /R /D Y", Logger, "Takeown1");
            res += RunCommand("icacls", $"\"{DirectoryPath}\" /grant Administrators:F /T", Logger, "Takeown2");
            res += RunCommand("icacls", $"\"{DirectoryPath}\" /inheritance:e /T", Logger, "Takeown3");
            res += RunCommand("icacls", $"\"{DirectoryPath}\" /reset /T", Logger, "Takeown4");

            if (res != 0)
            {
                Logger.LogError("Failed to restore permissions for directory {DirectoryPath}. Takeown result: {TakeownResult}", DirectoryPath, res);
            }
        }

        public override void Dispose()
        {
            RestoreAllPermissions();
            base.Dispose();
        }
    }

    public class TempDirectory : IDisposable
    {
        public string DirectoryPath { get; }
        public DirectoryInfo DirectoryInfo { get; }

        public static TempDirectory Create()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var directoryInfo = Directory.CreateDirectory(directoryPath);
            return new TempDirectory(directoryInfo);
        }

        public static TempDirectoryRestrictedPermissions CreateWithNoPermissions(ILogger logger)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), $"ShadowCopy{Path.GetRandomFileName()}");
            var directoryInfo = Directory.CreateDirectory(directoryPath);
            return new TempDirectoryRestrictedPermissions(directoryInfo, logger, false);
        }

        public static TempDirectoryRestrictedPermissions CreateWithNoWritePermissions(ILogger logger)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), $"ShadowCopy{Path.GetRandomFileName()}");
            var directoryInfo = Directory.CreateDirectory(directoryPath);
            return new TempDirectoryRestrictedPermissions(directoryInfo, logger, true);
        }

        public TempDirectory(DirectoryInfo directoryInfo)
        {
            DirectoryPath = directoryInfo.FullName;
            DirectoryInfo = directoryInfo;
        }

        public virtual void Dispose()
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
