// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

/// <summary>
/// Parameters to control application deployment.
/// </summary>
public class DeploymentParameters
{
    public DeploymentParameters()
    {
        EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "true";

        var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
        if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
        {
            Configuration = configAttribute.Configuration;
        }
    }

    public DeploymentParameters(TestVariant variant)
    {
        EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "true";

        var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
        if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
        {
            Configuration = configAttribute.Configuration;
        }

        ServerType = variant.Server;
        TargetFramework = variant.Tfm;
        ApplicationType = variant.ApplicationType;
        RuntimeArchitecture = variant.Architecture;
        HostingModel = variant.HostingModel;
    }

    /// <summary>
    /// Creates an instance of <see cref="DeploymentParameters"/>.
    /// </summary>
    /// <param name="applicationPath">Source code location of the target location to be deployed.</param>
    /// <param name="serverType">Where to be deployed on.</param>
    /// <param name="runtimeFlavor">Flavor of the clr to run against.</param>
    /// <param name="runtimeArchitecture">Architecture of the runtime to be used.</param>
    public DeploymentParameters(
        string applicationPath,
        ServerType serverType,
        RuntimeFlavor runtimeFlavor,
        RuntimeArchitecture runtimeArchitecture)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationPath);

        if (!Directory.Exists(applicationPath))
        {
            throw new DirectoryNotFoundException($"Application path {applicationPath} does not exist.");
        }

        ApplicationPath = applicationPath;
        ApplicationName = new DirectoryInfo(ApplicationPath).Name;
        ServerType = serverType;
        RuntimeFlavor = runtimeFlavor;
        EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "true";

        var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
        if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
        {
            Configuration = configAttribute.Configuration;
        }
    }

    public DeploymentParameters(DeploymentParameters parameters)
    {
        foreach (var propertyInfo in typeof(DeploymentParameters).GetProperties())
        {
            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(this, propertyInfo.GetValue(parameters));
            }
        }

        foreach (var kvp in parameters.EnvironmentVariables)
        {
            EnvironmentVariables.Add(kvp);
        }

        foreach (var kvp in parameters.PublishEnvironmentVariables)
        {
            PublishEnvironmentVariables.Add(kvp);
        }
    }

    public ApplicationPublisher ApplicationPublisher { get; set; }

    public ServerType ServerType { get; set; }

    public RuntimeFlavor RuntimeFlavor { get; set; }

    public RuntimeArchitecture RuntimeArchitecture { get; set; } = RuntimeArchitectures.Current;

    /// <summary>
    /// Suggested base url for the deployed application. The final deployed url could be
    /// different than this. Use <see cref="DeploymentResult.ApplicationBaseUri"/> for the
    /// deployed url.
    /// </summary>
    public string ApplicationBaseUriHint { get; set; }

    public bool RestoreDependencies { get; set; }

    /// <summary>
    /// Scheme used by the deployed application if <see cref="ApplicationBaseUriHint"/> is empty.
    /// </summary>
    public string Scheme { get; set; } = Uri.UriSchemeHttp;

    public string EnvironmentName { get; set; }

    public string ServerConfigTemplateContent { get; set; }

    public string ServerConfigLocation { get; set; }

    public string SiteName { get; set; } = "HttpTestSite";

    public string ApplicationPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the application. This is used to execute the application when deployed.
    /// Defaults to the file name of <see cref="ApplicationPath"/>.
    /// </summary>
    public string ApplicationName { get; set; }

    public string TargetFramework { get; set; }

    /// <summary>
    /// Configuration under which to build (ex: Release or Debug)
    /// </summary>
    public string Configuration { get; set; } = "Debug";

    /// <summary>
    /// Space separated command line arguments to be passed to dotnet-publish
    /// </summary>
    public string AdditionalPublishParameters { get; set; }

    /// <summary>
    /// To publish the application before deployment.
    /// </summary>
    public bool PublishApplicationBeforeDeployment { get; set; }

    public bool PreservePublishedApplicationForDebugging { get; set; }

    public bool StatusMessagesEnabled { get; set; } = true;

    public ApplicationType ApplicationType { get; set; }

    public string PublishedApplicationRootPath { get; set; }

    public HostingModel HostingModel { get; set; }

    /// <summary>
    /// Environment variables to be set before starting the host.
    /// Not applicable for IIS Scenarios.
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Environment variables used when invoking dotnet publish.
    /// </summary>
    public IDictionary<string, string> PublishEnvironmentVariables { get; } = new Dictionary<string, string>();

    /// <summary>
    /// For any application level cleanup to be invoked after performing host cleanup.
    /// </summary>
    public Action<DeploymentParameters> UserAdditionalCleanup { get; set; }

    /// <summary>
    /// Timeout for publish
    /// </summary>
    public TimeSpan? PublishTimeout { get; set; }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "[Variation] :: ServerType={0}, Runtime={1}, Arch={2}, BaseUrlHint={3}, Publish={4}",
            ServerType,
            RuntimeFlavor,
            RuntimeArchitecture,
            ApplicationBaseUriHint,
            PublishApplicationBeforeDeployment);
    }
}
