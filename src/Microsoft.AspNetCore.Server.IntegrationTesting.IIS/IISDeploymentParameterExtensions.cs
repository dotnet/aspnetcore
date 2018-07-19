// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            parameters.ServerConfigActionList.Add(action);
        }

        public static void ModifyWebConfig(this IISDeploymentParameters parameters, Action<XElement> transform)
        {
            parameters.WebConfigActionList.Add(transform);
        }

        public static void ModifyAspNetCoreSectionInWebConfig(this IISDeploymentParameters parameters, string key, string value)
            => ModifyAttributeInWebConfig(parameters, key, value, section: "aspNetCore");


        public static void ModifyAttributeInWebConfig(this IISDeploymentParameters parameters, string key, string value, string section)
        {
            parameters.WebConfigActionList.Add((element) =>
            {
                element.Descendants(section).SingleOrDefault().SetAttributeValue(key, value);
            });
        }

        public static void AddHttpsToServerConfig(this IISDeploymentParameters parameters)
        {
            parameters.ServerConfigActionList.Add(
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
            parameters.ServerConfigActionList.Add(
                element =>
                {
                    element.Descendants("windowsAuthentication")
                        .Single()
                        .SetAttributeValue("enabled", "true");
                });
        }

        public static void ModifyHandlerSectionInWebConfig(this IISDeploymentParameters parameters, string key, string value)
        {
            parameters.WebConfigActionList.Add(element =>
            {
                element.Descendants("handlers")
                        .FirstOrDefault()
                        .Descendants("add")
                        .FirstOrDefault()
                        .SetAttributeValue(key, value);
            });
        }
    }
}
