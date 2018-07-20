// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        public IISDeployerBase(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
            IISDeploymentParameters = deploymentParameters;
        }

        public void RunWebConfigActions()
        {
            if (IISDeploymentParameters == null)
            {
                return;
            }

            if (!DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                throw new InvalidOperationException("Cannot modify web.config file if no published output.");
            }

            var path = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "web.config");
            var webconfig = XDocument.Load(path);
            var xElement = webconfig.Descendants("system.webServer").Single();
            foreach (var action in IISDeploymentParameters.WebConfigActionList)
            {
                action.Invoke(xElement);
            }

            webconfig.Save(path);
        }

        public string RunServerConfigActions(string serverConfigString)
        {
            if (IISDeploymentParameters == null)
            {
                return serverConfigString;
            }

            var serverConfig = XDocument.Parse(serverConfigString);
            var xElement = serverConfig.Descendants("configuration").FirstOrDefault();

            foreach (var action in IISDeploymentParameters.ServerConfigActionList)
            {
                action.Invoke(xElement);
            }
            return xElement.ToString();
        }
    }
}
