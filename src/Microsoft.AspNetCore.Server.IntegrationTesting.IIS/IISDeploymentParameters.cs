// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public class IISDeploymentParameters : DeploymentParameters
    {
        public IISDeploymentParameters() : base()
        {
            WebConfigActionList = CreateDefaultWebConfigActionList();
        }

        public IISDeploymentParameters(TestVariant variant)
            : base(variant)
        {
            WebConfigActionList = CreateDefaultWebConfigActionList();
        }

        public IISDeploymentParameters(
           string applicationPath,
           ServerType serverType,
           RuntimeFlavor runtimeFlavor,
           RuntimeArchitecture runtimeArchitecture)
            : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
        {
            WebConfigActionList = CreateDefaultWebConfigActionList();
        }

        public IISDeploymentParameters(DeploymentParameters parameters)
            : base(parameters)
        {
            WebConfigActionList = CreateDefaultWebConfigActionList();

            if (parameters is IISDeploymentParameters)
            {
                var tempParameters = (IISDeploymentParameters)parameters;
                WebConfigActionList = tempParameters.WebConfigActionList;
                ServerConfigActionList = tempParameters.ServerConfigActionList;
                WebConfigBasedEnvironmentVariables = tempParameters.WebConfigBasedEnvironmentVariables;
                HandlerSettings = tempParameters.HandlerSettings;
                GracefulShutdown = tempParameters.GracefulShutdown;
            }
        }

        private IList<Action<XElement, string>> CreateDefaultWebConfigActionList()
        {
            return new List<Action<XElement, string>>() { AddWebConfigEnvironmentVariables(), AddHandlerSettings() };
        }

        public IList<Action<XElement, string>> WebConfigActionList { get; }

        public IList<Action<XElement, string>> ServerConfigActionList { get; } = new List<Action<XElement, string>>();

        public IDictionary<string, string> WebConfigBasedEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public IDictionary<string, string> HandlerSettings { get; set; } = new Dictionary<string, string>();

        public bool GracefulShutdown { get; set; }

        private Action<XElement, string> AddWebConfigEnvironmentVariables()
        {
            return (element, _) =>
            {
                if (WebConfigBasedEnvironmentVariables.Count == 0)
                {
                    return;
                }

                var environmentVariables = element
                    .RequiredElement("system.webServer")
                    .RequiredElement("aspNetCore")
                    .GetOrAdd("environmentVariables");


                foreach (var envVar in WebConfigBasedEnvironmentVariables)
                {
                    environmentVariables.GetOrAdd("environmentVariable", "name", envVar.Key)
                        .SetAttributeValue("value", envVar.Value);
                }
            };
        }

        private Action<XElement, string> AddHandlerSettings()
        {
            return (element, _) =>
            {
                if (HandlerSettings.Count == 0)
                {
                    return;
                }

                var handlerSettings = element
                    .RequiredElement("system.webServer")
                    .RequiredElement("aspNetCore")
                    .GetOrAdd("handlerSettings");

                foreach (var handlerSetting in HandlerSettings)
                {
                    handlerSettings.GetOrAdd("handlerSetting", "name", handlerSetting.Key)
                        .SetAttributeValue("value", handlerSetting.Value);
                }
            };
        }
    }
}
