// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
internal sealed class ProjectIdResolver
{
    private const string DefaultConfig = "Debug";
    private readonly IReporter _reporter;
    private readonly string _targetsFile;
    private readonly string _workingDirectory;

    public ProjectIdResolver(IReporter reporter, string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _reporter = reporter;
        _targetsFile = FindTargetsFile();
    }

    public string Resolve(string project, string configuration)
    {
        var finder = new MsBuildProjectFinder(_workingDirectory);
        string projectFile;
        try
        {
            projectFile = finder.FindMsBuildProject(project);
        }
        catch (Exception ex)
        {
            _reporter.Error(ex.Message);
            return null;
        }

        _reporter.Verbose(SecretsHelpersResources.FormatMessage_Project_File_Path(projectFile));

        configuration = !string.IsNullOrEmpty(configuration)
            ? configuration
            : DefaultConfig;

        var outputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = DotNetMuxer.MuxerPathOrDefault(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                ArgumentList =
                    {
                        "msbuild",
                        projectFile,
                        "/nologo",
                        "/t:_ExtractUserSecretsMetadata", // defined in SecretManager.targets
                        "/p:_UserSecretsMetadataFile=" + outputFile,
                        "/p:Configuration=" + configuration,
                        "/p:CustomAfterMicrosoftCommonTargets=" + _targetsFile,
                        "/p:CustomAfterMicrosoftCommonCrossTargetingTargets=" + _targetsFile,
                        "-verbosity:detailed",
                    }
            };

#if DEBUG
            _reporter.Verbose($"Invoking '{psi.FileName} {psi.Arguments}'");
#endif

            using var process = new Process()
            {
                StartInfo = psi,
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, d) =>
            {
                if (!string.IsNullOrEmpty(d.Data))
                {
                    outputBuilder.AppendLine(d.Data);
                }
            };
            process.ErrorDataReceived += (_, d) =>
            {
                if (!string.IsNullOrEmpty(d.Data))
                {
                    errorBuilder.AppendLine(d.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                _reporter.Verbose(outputBuilder.ToString());
                _reporter.Verbose(errorBuilder.ToString());
                _reporter.Error($"Exit code: {process.ExitCode}");
                _reporter.Error(SecretsHelpersResources.FormatError_ProjectFailedToLoad(projectFile));
                return null;
            }

            if (!File.Exists(outputFile))
            {
                _reporter.Error(SecretsHelpersResources.FormatError_ProjectMissingId(projectFile));
                return null;
            }

            var id = File.ReadAllText(outputFile)?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                _reporter.Error(SecretsHelpersResources.FormatError_ProjectMissingId(projectFile));
            }
            return id;

        }
        finally
        {
            TryDelete(outputFile);
        }
    }

    private string FindTargetsFile()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(ProjectIdResolver).Assembly.Location);
        var searchPaths = new[]
        {
                Path.Combine(AppContext.BaseDirectory, "assets"),
                Path.Combine(assemblyDir, "assets"),
                AppContext.BaseDirectory,
                assemblyDir,
            };

        var targetPath = searchPaths.Select(p => Path.Combine(p, "SecretManager.targets")).FirstOrDefault(File.Exists);
        if (targetPath == null)
        {
            _reporter.Error("Fatal error: could not find SecretManager.targets");
            return null;
        }
        return targetPath;
    }

    private static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // whatever
        }
    }
}
