// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class RemoteWindowsDeployer : ApplicationDeployer
{
    /// <summary>
    /// Example: If the share path is '\\dir1\dir2', then this returns the full path to the
    /// deployed folder. Example: '\\dir1\dir2\048f6c99-de3e-488a-8020-f9eb277818d9'
    /// </summary>
    private string _deployedFolderPathInFileShare;
    private readonly RemoteWindowsDeploymentParameters _deploymentParameters;
    private bool _isDisposed;
    private static readonly Lazy<Scripts> _scripts = new Lazy<Scripts>(CopyEmbeddedScriptFilesToDisk);

    public RemoteWindowsDeployer(RemoteWindowsDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
        : base(deploymentParameters, loggerFactory)
    {
        _deploymentParameters = deploymentParameters;

        if (_deploymentParameters.ServerType != ServerType.IIS
            && _deploymentParameters.ServerType != ServerType.Kestrel
            && _deploymentParameters.ServerType != ServerType.HttpSys)
        {
            throw new InvalidOperationException($"Server type {_deploymentParameters.ServerType} is not supported for remote deployment." +
                $" Supported server types are {nameof(ServerType.Kestrel)}, {nameof(ServerType.IIS)} and {nameof(ServerType.HttpSys)}");
        }

        if (string.IsNullOrEmpty(_deploymentParameters.ServerName))
        {
            throw new ArgumentException($"Invalid value '{_deploymentParameters.ServerName}' for {nameof(RemoteWindowsDeploymentParameters.ServerName)}");
        }

        if (string.IsNullOrEmpty(_deploymentParameters.ServerAccountName))
        {
            throw new ArgumentException($"Invalid value '{_deploymentParameters.ServerAccountName}' for {nameof(RemoteWindowsDeploymentParameters.ServerAccountName)}." +
                " Account credentials are required to enable creating a powershell session to the remote server.");
        }

        if (string.IsNullOrEmpty(_deploymentParameters.ServerAccountPassword))
        {
            throw new ArgumentException($"Invalid value '{_deploymentParameters.ServerAccountPassword}' for {nameof(RemoteWindowsDeploymentParameters.ServerAccountPassword)}." +
                " Account credentials are required to enable creating a powershell session to the remote server.");
        }

        if (_deploymentParameters.ApplicationType == ApplicationType.Portable
            && string.IsNullOrEmpty(_deploymentParameters.DotnetRuntimePath))
        {
            throw new ArgumentException($"Invalid value '{_deploymentParameters.DotnetRuntimePath}' for {nameof(RemoteWindowsDeploymentParameters.DotnetRuntimePath)}. " +
                "It must be non-empty for portable apps.");
        }

        if (string.IsNullOrEmpty(_deploymentParameters.RemoteServerFileSharePath))
        {
            throw new ArgumentException($"Invalid value for {nameof(RemoteWindowsDeploymentParameters.RemoteServerFileSharePath)}." +
                " . A file share is required to copy the application's published output.");
        }

        if (string.IsNullOrEmpty(_deploymentParameters.ApplicationBaseUriHint))
        {
            throw new ArgumentException($"Invalid value for {nameof(RemoteWindowsDeploymentParameters.ApplicationBaseUriHint)}.");
        }
    }

    public override async Task<DeploymentResult> DeployAsync()
    {
        using (Logger.BeginScope("Deploy"))
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name, "This instance of deployer has already been disposed.");
            }

            // Publish the app to a local temp folder on the machine where the test is running
            DotnetPublish();

            if (_deploymentParameters.ServerType == ServerType.IIS)
            {
                UpdateWebConfig();
            }

            var folderId = Guid.NewGuid().ToString();
            _deployedFolderPathInFileShare = Path.Combine(_deploymentParameters.RemoteServerFileSharePath, folderId);

            DirectoryCopy(
                _deploymentParameters.PublishedApplicationRootPath,
                _deployedFolderPathInFileShare,
                copySubDirs: true);
            Logger.LogInformation($"Copied the locally published folder to the file share path '{_deployedFolderPathInFileShare}'");

            await RunScriptAsync("StartServer");

            return new DeploymentResult(
                LoggerFactory,
                DeploymentParameters,
                DeploymentParameters.ApplicationBaseUriHint);
        }
    }

    public override void Dispose()
    {
        using (Logger.BeginScope("Dispose"))
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            try
            {
                Logger.LogInformation($"Stopping the application on the server '{_deploymentParameters.ServerName}'");
                RunScriptAsync("StopServer").Wait();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(0, ex, "Failed to stop the server.");
            }

            try
            {
                Logger.LogInformation($"Deleting the deployed folder '{_deployedFolderPathInFileShare}'");
                Directory.Delete(_deployedFolderPathInFileShare, recursive: true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(0, ex, $"Failed to delete the deployed folder '{_deployedFolderPathInFileShare}'.");
            }

            try
            {
                Logger.LogInformation($"Deleting the locally published folder '{DeploymentParameters.PublishedApplicationRootPath}'");
                Directory.Delete(DeploymentParameters.PublishedApplicationRootPath, recursive: true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(0, ex, $"Failed to delete the locally published folder '{DeploymentParameters.PublishedApplicationRootPath}'.");
            }
        }
    }

    private void UpdateWebConfig()
    {
        var webConfigFilePath = Path.Combine(_deploymentParameters.PublishedApplicationRootPath, "web.config");
        var webConfig = XDocument.Load(webConfigFilePath);
        var aspNetCoreSection = webConfig.Descendants("aspNetCore")
            .Single();

        // if the dotnet runtime path is specified, update the published web.config file to have that path
        if (!string.IsNullOrEmpty(_deploymentParameters.DotnetRuntimePath))
        {
            aspNetCoreSection.SetAttributeValue(
                "processPath",
                Path.Combine(_deploymentParameters.DotnetRuntimePath, "dotnet.exe"));
        }

        var environmentVariablesSection = aspNetCoreSection.Elements("environmentVariables").FirstOrDefault();
        if (environmentVariablesSection == null)
        {
            environmentVariablesSection = new XElement("environmentVariables");
            aspNetCoreSection.Add(environmentVariablesSection);
        }

        foreach (var envVariablePair in _deploymentParameters.EnvironmentVariables)
        {
            var environmentVariable = new XElement("environmentVariable");
            environmentVariable.SetAttributeValue("name", envVariablePair.Key);
            environmentVariable.SetAttributeValue("value", envVariablePair.Value);
            environmentVariablesSection.Add(environmentVariable);
        }

        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace($"Config File Content:{Environment.NewLine}===START CONFIG==={Environment.NewLine}{{configContent}}{Environment.NewLine}===END CONFIG===", webConfig.ToString());
        }

        using (var fileStream = File.Open(webConfigFilePath, FileMode.Open))
        {
            webConfig.Save(fileStream);
        }
    }

    private async Task RunScriptAsync(string serverAction)
    {
        using (Logger.BeginScope($"RunScript:{serverAction}"))
        {
            var remotePSSessionHelperScript = _scripts.Value.RemotePSSessionHelper;

            string executablePath = null;
            string executableParameters = null;
            var applicationName = new DirectoryInfo(DeploymentParameters.ApplicationPath).Name;
            if (DeploymentParameters.ApplicationType == ApplicationType.Portable)
            {
                executablePath = "dotnet.exe";
                executableParameters = Path.Combine(_deployedFolderPathInFileShare, applicationName + ".dll");
            }
            else
            {
                executablePath = Path.Combine(_deployedFolderPathInFileShare, applicationName + ".exe");
            }

            var parameterBuilder = new StringBuilder();
            parameterBuilder.Append(FormattableString.Invariant($"\"{remotePSSessionHelperScript}\""));
            parameterBuilder.Append(FormattableString.Invariant($" -serverName {_deploymentParameters.ServerName}"));
            parameterBuilder.Append(FormattableString.Invariant($" -accountName {_deploymentParameters.ServerAccountName}"));
            parameterBuilder.Append(FormattableString.Invariant($" -accountPassword {_deploymentParameters.ServerAccountPassword}"));
            parameterBuilder.Append(FormattableString.Invariant($" -deployedFolderPath {_deployedFolderPathInFileShare}"));

            if (!string.IsNullOrEmpty(_deploymentParameters.DotnetRuntimePath))
            {
                parameterBuilder.Append(FormattableString.Invariant($" -dotnetRuntimePath \"{_deploymentParameters.DotnetRuntimePath}\""));
            }

            parameterBuilder.Append(FormattableString.Invariant($" -executablePath \"{executablePath}\""));

            if (!string.IsNullOrEmpty(executableParameters))
            {
                parameterBuilder.Append(FormattableString.Invariant($" -executableParameters \"{executableParameters}\""));
            }

            parameterBuilder.Append(FormattableString.Invariant($" -serverType {_deploymentParameters.ServerType}"));
            parameterBuilder.Append(FormattableString.Invariant($" -serverAction {serverAction}"));
            parameterBuilder.Append(FormattableString.Invariant($" -applicationBaseUrl {_deploymentParameters.ApplicationBaseUriHint}"));
            var environmentVariables = string.Join("`,", _deploymentParameters.EnvironmentVariables.Select(envVariable => $"{envVariable.Key}={envVariable.Value}"));
            parameterBuilder.Append(FormattableString.Invariant($" -environmentVariables \"{environmentVariables}\""));

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = parameterBuilder.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            using (var runScriptsOnRemoteServerProcess = new Process() { StartInfo = startInfo })
            {
                runScriptsOnRemoteServerProcess.EnableRaisingEvents = true;
                runScriptsOnRemoteServerProcess.Exited += (sender, exitedArgs) =>
                {
                    Logger.LogInformation($"[{_deploymentParameters.ServerName} {serverAction} stdout]: script complete");
                };

                runScriptsOnRemoteServerProcess.StartAndCaptureOutAndErrToLogger(serverAction, Logger);

                // Wait a minute for the script to run or fail. The StartServer script will only terminate when the
                // Deployer is disposed, so we don't want to wait for it to terminate here because it would deadlock.
                await Task.Delay(TimeSpan.FromMinutes(1));

                if (runScriptsOnRemoteServerProcess.HasExited && runScriptsOnRemoteServerProcess.ExitCode != 0)
                {
                    throw new Exception($"Failed to execute the script on '{_deploymentParameters.ServerName}'.");
                }
            }
        }
    }

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

    private static Scripts CopyEmbeddedScriptFilesToDisk()
    {
        var embeddedFileNames = new[] { "RemotePSSessionHelper.ps1", "StartServer.ps1", "StopServer.ps1" };

        // Copy the scripts from this assembly's embedded resources to the temp path on the machine where these
        // tests are being run
        var assembly = typeof(RemoteWindowsDeployer).GetTypeInfo().Assembly;
        var embeddedFileProvider = new EmbeddedFileProvider(
            assembly,
            $"{assembly.GetName().Name}.Deployers.RemoteWindowsDeployer");

        var filesOnDisk = new string[embeddedFileNames.Length];
        for (var i = 0; i < embeddedFileNames.Length; i++)
        {
            var embeddedFileName = embeddedFileNames[i];
            var physicalFilePath = Path.Combine(Path.GetTempPath(), embeddedFileName);
            var sourceStream = embeddedFileProvider
                .GetFileInfo(embeddedFileName)
                .CreateReadStream();

            using (sourceStream)
            {
                var destinationStream = File.Create(physicalFilePath);
                using (destinationStream)
                {
                    sourceStream.CopyTo(destinationStream);
                }
            }

            filesOnDisk[i] = physicalFilePath;
        }

        var scripts = new Scripts(filesOnDisk[0], filesOnDisk[1], filesOnDisk[2]);

        return scripts;
    }

    private sealed class Scripts
    {
        public Scripts(string remotePSSessionHelper, string startServer, string stopServer)
        {
            RemotePSSessionHelper = remotePSSessionHelper;
            StartServer = startServer;
            StopServer = stopServer;
        }

        public string RemotePSSessionHelper { get; }

        public string StartServer { get; }

        public string StopServer { get; }
    }
}
