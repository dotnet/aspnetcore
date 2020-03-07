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
        protected const string AspNetCoreModuleV2ModuleName = "AspNetCoreModuleV2";

        public IISDeploymentParameters IISDeploymentParameters { get; }

        public IISDeployerBase(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
            IISDeploymentParameters = deploymentParameters;
        }

        protected void RunWebConfigActions(string contentRoot)
        {
            var actions = GetWebConfigActions();
            if (!actions.Any())
            {
                return;
            }

            if (!DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                throw new InvalidOperationException("Cannot modify web.config file if no published output.");
            }

            var path = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "web.config");
            var webconfig = XDocument.Load(path);

            foreach (var action in actions)
            {
                action.Invoke(webconfig.Root, contentRoot);
            }

            webconfig.Save(path);
        }

        protected virtual IEnumerable<Action<XElement, string>> GetWebConfigActions()
        {
            if (IISDeploymentParameters.HandlerSettings.Any())
            {
                yield return AddHandlerSettings;
            }

            if (IISDeploymentParameters.WebConfigBasedEnvironmentVariables.Any())
            {
                yield return AddWebConfigEnvironmentVariables;
            }

            foreach (var action in IISDeploymentParameters.WebConfigActionList)
            {
                yield return action;
            }
        }

        protected virtual IEnumerable<Action<XElement, string>> GetServerConfigActions()
        {
            foreach (var action in IISDeploymentParameters.ServerConfigActionList)
            {
                yield return action;
            }
        }

        public void RunServerConfigActions(XElement config, string contentRoot)
        {
            foreach (var action in GetServerConfigActions())
            {
                action.Invoke(config, contentRoot);
            }
        }

        protected string GetAncmLocation()
        {
            var ancmDllName = "aspnetcorev2.dll";
            // There are issues with having multiple dlls copy to the same location in both build and publish
            // It's inherently racy. Therefore, we have two different copy locations and when trying verify backwards compat tests,
            // we select the version of ANCM in a different folder.
            var basePath = File.Exists(Path.Combine(AppContext.BaseDirectory, "x64", "aspnetcorev2.dll")) ? "" : @"ANCM\";
            var arch = DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x64 ? $@"{basePath}x64\{ancmDllName}" : $@"{basePath}x86\{ancmDllName}";
            var ancmFile = Path.Combine(AppContext.BaseDirectory, arch);
            if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
            {
                ancmFile = Path.Combine(AppContext.BaseDirectory, ancmDllName);
                if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                {
                    throw new FileNotFoundException("AspNetCoreModuleV2 could not be found.", ancmFile);
                }
            }

            return ancmFile;
        }

        private void AddWebConfigEnvironmentVariables(XElement element, string contentRoot)
        {
            var environmentVariables = element
                .Descendants("system.webServer")
                .Single()
                .RequiredElement("aspNetCore")
                .GetOrAdd("environmentVariables");

            foreach (var envVar in IISDeploymentParameters.WebConfigBasedEnvironmentVariables)
            {
                environmentVariables.GetOrAdd("environmentVariable", "name", envVar.Key)
                    .SetAttributeValue("value", envVar.Value);
            }
        }

        private void AddHandlerSettings(XElement element, string contentRoot)
        {
            var handlerSettings = element
                .Descendants("system.webServer")
                .Single()
                .RequiredElement("aspNetCore")
                .GetOrAdd("handlerSettings");

            foreach (var handlerSetting in IISDeploymentParameters.HandlerSettings)
            {
                handlerSettings.GetOrAdd("handlerSetting", "name", handlerSetting.Key)
                    .SetAttributeValue("value", handlerSetting.Value);
            }
        }

        protected void ConfigureModuleAndBinding(XElement config, string contentRoot, int port)
        {
            var siteElement = config
                .RequiredElement("system.applicationHost")
                .RequiredElement("sites")
                .RequiredElement("site");

            siteElement
                .RequiredElement("application")
                .RequiredElement("virtualDirectory")
                .SetAttributeValue("physicalPath", contentRoot);

            siteElement
                .RequiredElement("bindings")
                .RequiredElement("binding")
                .SetAttributeValue("bindingInformation", $":{port}:localhost");

            config
                .RequiredElement("system.webServer")
                .RequiredElement("globalModules")
                .GetOrAdd("add", "name", AspNetCoreModuleV2ModuleName)
                .SetAttributeValue("image", GetAncmLocation());
        }

        public abstract void Dispose(bool gracefulShutdown);
    }
}
