// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class CachingApplicationPublisher : ApplicationPublisher, IDisposable
{
    private readonly Dictionary<DotnetPublishParameters, PublishedApplication> _publishCache = new Dictionary<DotnetPublishParameters, PublishedApplication>();

    public CachingApplicationPublisher(string applicationPath) : base(applicationPath)
    {
    }

    public override async Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
    {
        if (ApplicationPath != deploymentParameters.ApplicationPath)
        {
            throw new InvalidOperationException("ApplicationPath mismatch");
        }

        if (deploymentParameters.PublishEnvironmentVariables.Any())
        {
            throw new InvalidOperationException("DeploymentParameters.PublishEnvironmentVariables not supported");
        }

        if (!string.IsNullOrEmpty(deploymentParameters.PublishedApplicationRootPath))
        {
            throw new InvalidOperationException("DeploymentParameters.PublishedApplicationRootPath not supported");
        }

        var dotnetPublishParameters = new DotnetPublishParameters
        {
            TargetFramework = deploymentParameters.TargetFramework,
            Configuration = deploymentParameters.Configuration,
            ApplicationType = deploymentParameters.ApplicationType,
            RuntimeArchitecture = deploymentParameters.RuntimeArchitecture
        };

        if (!_publishCache.TryGetValue(dotnetPublishParameters, out var publishedApplication))
        {
            publishedApplication = await base.Publish(deploymentParameters, logger);
            _publishCache.Add(dotnetPublishParameters, publishedApplication);
        }

        return new PublishedApplication(CopyPublishedOutput(publishedApplication, logger), logger);
    }

    private static string CopyPublishedOutput(PublishedApplication application, ILogger logger)
    {
        var target = CreateTempDirectory();

        var source = new DirectoryInfo(application.Path);
        CopyFiles(source, target, logger);
        return target.FullName;
    }

    public static void CopyFiles(DirectoryInfo source, DirectoryInfo target, ILogger logger)
    {
        foreach (DirectoryInfo directoryInfo in source.GetDirectories())
        {
            CopyFiles(directoryInfo, target.CreateSubdirectory(directoryInfo.Name), logger);
        }

        logger.LogDebug($"Processing {target.FullName}");
        foreach (FileInfo fileInfo in source.GetFiles())
        {
            logger.LogDebug($"  Copying {fileInfo.Name}");
            var destFileName = Path.Combine(target.FullName, fileInfo.Name);
            fileInfo.CopyTo(destFileName);
        }
    }

    public void Dispose()
    {
        foreach (var publishedApp in _publishCache.Values)
        {
            publishedApp.Dispose();
        }
    }

    private struct DotnetPublishParameters
    {
        public string TargetFramework { get; set; }
        public string Configuration { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public RuntimeArchitecture RuntimeArchitecture { get; set; }
    }
}
