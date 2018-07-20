// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

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

        private IList<Action<XElement>> CreateDefaultWebConfigActionList()
        {
            return new List<Action<XElement>>() { AddWebConfigEnvironmentVariables(), AddHandlerSettings() };
        }

        public IList<Action<XElement>> WebConfigActionList { get; }

        public IList<Action<XElement>> ServerConfigActionList { get; } = new List<Action<XElement>>();

        public IDictionary<string, string> WebConfigBasedEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public IDictionary<string, string> HandlerSettings { get; set; } = new Dictionary<string, string>();

        public bool GracefulShutdown { get; set; }

        private Action<XElement> AddWebConfigEnvironmentVariables()
        {
            return xElement =>
            {
                if (WebConfigBasedEnvironmentVariables.Count == 0)
                {
                    return;
                }

                var element = xElement.Descendants("environmentVariables").SingleOrDefault();
                if (element == null)
                {
                    element = new XElement("environmentVariables");
                    xElement.Descendants("aspNetCore").SingleOrDefault().Add(element);
                }

                foreach (var envVar in WebConfigBasedEnvironmentVariables)
                {
                    CreateOrSetElement(element, envVar.Key, envVar.Value, "environmentVariable");
                }
            };
        }

        private Action<XElement> AddHandlerSettings()
        {
            return xElement =>
            {
                if (HandlerSettings.Count == 0)
                {
                    return;
                }

                var element = xElement.Descendants("handlerSettings").SingleOrDefault();
                if (element == null)
                {
                    element = new XElement("handlerSettings");
                    xElement.Descendants("aspNetCore").SingleOrDefault().Add(element);
                }

                foreach (var handlerSetting in HandlerSettings)
                {
                    CreateOrSetElement(element, handlerSetting.Key, handlerSetting.Value, "handlerSetting");
                }
            };
        }

        private static void CreateOrSetElement(XElement rootElement, string name, string value, string elementName)
        {
            if (rootElement.Descendants()
                .Attributes()
                .Where(attribute => attribute.Value == name)
                .Any())
            {
                return;
            }
            var element = new XElement(elementName);
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("value", value);
            rootElement.Add(element);
        }
    }
}
