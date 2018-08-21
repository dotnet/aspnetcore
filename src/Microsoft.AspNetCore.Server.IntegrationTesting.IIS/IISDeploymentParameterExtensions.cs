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
            parameters.HandlerSettings["debugLevel"] = "4";
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
    }
}
