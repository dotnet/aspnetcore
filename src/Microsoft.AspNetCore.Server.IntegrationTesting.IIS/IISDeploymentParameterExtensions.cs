// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public static class IISDeploymentParameterExtensions
    {
        public static void AddDebugLogToWebConfig(this IISDeploymentParameters parameters, string filename)
        {
            parameters.HandlerSettings["debugLevel"] = "file";
            parameters.HandlerSettings["debugFile"] = filename;
        }

        public static void AddServerConfigAction(this IISDeploymentParameters parameters, Action<XElement> action)
        {
            parameters.ServerConfigActionList.Add((config, _) => action(config));
        }

        public static void AddServerConfigAction(this IISDeploymentParameters parameters, Action<XElement, string> action)
        {
            parameters.ServerConfigActionList.Add(action);
        }

        public static void AddHttpsToServerConfig(this IISDeploymentParameters parameters)
        {
            parameters.AddServerConfigAction(
                element =>
                {
                    element.Descendants("binding")
                        .Single()
                        .SetAttributeValue("protocol", "https");

                    element.Descendants("access")
                        .Single()
                        .SetAttributeValue("sslFlags", "Ssl, SslNegotiateCert");
                });
        }

        public static void AddWindowsAuthToServerConfig(this IISDeploymentParameters parameters)
        {
            parameters.AddServerConfigAction(
                element =>
                {
                    element.Descendants("windowsAuthentication")
                        .Single()
                        .SetAttributeValue("enabled", "true");
                });
        }

        public static void EnableLogging(this IISDeploymentParameters deploymentParameters, string path)
        {
            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogEnabled", "true"));

            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogFile", Path.Combine(path, "std")));
        }

        public static void TransformPath(this IISDeploymentParameters parameters, Func<string, string, string> transformation)
        {
            parameters.WebConfigActionList.Add(
                (config, contentRoot) =>
                {
                    var aspNetCoreElement = config.Descendants("aspNetCore").Single();
                    aspNetCoreElement.SetAttributeValue("processPath", transformation((string)aspNetCoreElement.Attribute("processPath"), contentRoot));
                });
        }

        public static void TransformArguments(this IISDeploymentParameters parameters, Func<string, string, string> transformation)
        {
            parameters.WebConfigActionList.Add(
                (config, contentRoot) =>
                {
                    var aspNetCoreElement = config.Descendants("aspNetCore").Single();
                    aspNetCoreElement.SetAttributeValue("arguments", transformation((string)aspNetCoreElement.Attribute("arguments"), contentRoot));
                });
        }

        public static void EnableModule(this IISDeploymentParameters parameters, string moduleName, string modulePath)
        {
            if (parameters.ServerType == ServerType.IIS)
            {
                modulePath = modulePath.Replace("%IIS_BIN%", "%windir%\\System32\\inetsrv");
            }

            parameters.ServerConfigActionList.Add(
                (element, _) => {
                    var webServerElement = element
                        .RequiredElement("system.webServer");

                    webServerElement
                        .RequiredElement("globalModules")
                        .GetOrAdd("add", "name", moduleName)
                        .SetAttributeValue("image", modulePath);

                    (webServerElement.Element("modules") ??
                     element
                         .Element("location")
                         .RequiredElement("system.webServer")
                         .RequiredElement("modules"))
                        .GetOrAdd("add", "name", moduleName);
                });
        }
    }
}
