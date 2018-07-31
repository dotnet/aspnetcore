// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
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
            AncmVersion = variant.AncmVersion;
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
            if (string.IsNullOrEmpty(applicationPath))
            {
                throw new ArgumentException("Value cannot be null.", nameof(applicationPath));
            }

            if (!Directory.Exists(applicationPath))
            {
                throw new DirectoryNotFoundException(string.Format("Application path {0} does not exist.", applicationPath));
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

        public ServerType ServerType { get; set;  }

        public RuntimeFlavor RuntimeFlavor { get; set;  }

        public RuntimeArchitecture RuntimeArchitecture { get; set; } = RuntimeArchitecture.x64;

        /// <summary>
        /// Suggested base url for the deployed application. The final deployed url could be
        /// different than this. Use <see cref="DeploymentResult.ApplicationBaseUri"/> for the
        /// deployed url.
        /// </summary>
        public string ApplicationBaseUriHint { get; set; }

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
        /// Publish restores by default, this property opts out by default.
        /// </summary>
        public bool RestoreOnPublish { get; set; }

        /// <summary>
        /// To publish the application before deployment.
        /// </summary>
        public bool PublishApplicationBeforeDeployment { get; set; }

        public bool PreservePublishedApplicationForDebugging { get; set; } = false;

        public bool StatusMessagesEnabled { get; set; } = true;

        public ApplicationType ApplicationType { get; set; }

        public string PublishedApplicationRootPath { get; set; }

        public HostingModel HostingModel { get; set; }

        /// <summary>
        /// When using the IISExpressDeployer, determines whether to use the older or newer version
        /// of ANCM.
        /// </summary>
        public AncmVersion AncmVersion { get; set; } = AncmVersion.AspNetCoreModule;

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

        public override string ToString()
        {
            return string.Format(
                    "[Variation] :: ServerType={0}, Runtime={1}, Arch={2}, BaseUrlHint={3}, Publish={4}",
                    ServerType,
                    RuntimeFlavor,
                    RuntimeArchitecture,
                    ApplicationBaseUriHint,
                    PublishApplicationBeforeDeployment);
        }
    }
}
