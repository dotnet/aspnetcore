// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public abstract class IISDeployerBase : ApplicationDeployer
    {
        public IISDeploymentParameters IISDeploymentParameters { get; }

        protected List<Action<XElement, string>> DefaultWebConfigActions { get; } = new List<Action<XElement, string>>();

        protected List<Action<XElement, string>> DefaultServerConfigActions { get; } = new List<Action<XElement, string>>();

        public IISDeployerBase(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
            IISDeploymentParameters = deploymentParameters;
        }

        public void RunWebConfigActions(string contentRoot)
        {
            if (!DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                throw new InvalidOperationException("Cannot modify web.config file if no published output.");
            }

            var path = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "web.config");
            var webconfig = XDocument.Load(path);

            foreach (var action in DefaultWebConfigActions)
            {
                action.Invoke(webconfig.Root, contentRoot);
            }

            if (IISDeploymentParameters != null)
            {
                foreach (var action in IISDeploymentParameters.WebConfigActionList)
                {
                    action.Invoke(webconfig.Root, contentRoot);
                }
            }

            webconfig.Save(path);
        }


        public void RunServerConfigActions(XElement config, string contentRoot)
        {
            foreach (var action in DefaultServerConfigActions)
            {
                action.Invoke(config, contentRoot);
            }

            if (IISDeploymentParameters != null)
            {
                foreach (var action in IISDeploymentParameters.ServerConfigActionList)
                {
                    action.Invoke(config, contentRoot);
                }
            }
        }

        protected string GetAncmLocation(AncmVersion version)
        {
            var ancmDllName = version == AncmVersion.AspNetCoreModuleV2 ? "aspnetcorev2.dll" : "aspnetcore.dll";
            var arch = DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x64 ? $@"x64\{ancmDllName}" : $@"x86\{ancmDllName}";
            var ancmFile = Path.Combine(AppContext.BaseDirectory, arch);
            if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
            {
                ancmFile = Path.Combine(AppContext.BaseDirectory, ancmDllName);
                if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                {
                    throw new FileNotFoundException("AspNetCoreModule could not be found.", ancmFile);
                }
            }

            return ancmFile;
        }
    }
}
