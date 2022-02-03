// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

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
                    .SetAttributeValue("sslFlags", "None");
            });
    }

    public static void AddHttpsWithClientCertToServerConfig(this IISDeploymentParameters parameters)
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

    public static void SetWindowsAuth(this IISDeploymentParameters parameters, bool enabled = true)
    {
        parameters.EnsureSection("windowsAuthentication", "system.webServer", "security", "windowsAuthentication");
        parameters.EnableModule("WindowsAuthenticationModule", "%IIS_BIN%\\authsspi.dll");

        parameters.AddServerConfigAction(
            element =>
            {
                var windowsAuthentication = element
                    .RequiredElement("system.webServer")
                    .RequiredElement("security")
                    .RequiredElement("authentication")
                    .GetOrAdd("windowsAuthentication");

                windowsAuthentication.SetAttributeValue("enabled", enabled);
                var providers = windowsAuthentication.GetOrAdd("providers");
                providers.GetOrAdd("add", "value", "Negotiate");
                providers.GetOrAdd("add", "value", "NTLM");
            });
    }

    public static void SetAnonymousAuth(this IISDeploymentParameters parameters, bool enabled = true)
    {
        parameters.AddServerConfigAction(
            element =>
            {
                element
                    .RequiredElement("system.webServer")
                    .RequiredElement("security")
                    .RequiredElement("authentication")
                    .GetOrAdd("anonymousAuthentication")
                    .SetAttributeValue("enabled", enabled);
            });
    }

    public static void SetBasicAuth(this IISDeploymentParameters parameters, bool enabled = true)
    {
        parameters.EnableModule("BasicAuthenticationModule", "%IIS_BIN%\\authbas.dll");

        parameters.AddServerConfigAction(
            element =>
            {
                element
                    .RequiredElement("system.webServer")
                    .RequiredElement("security")
                    .RequiredElement("authentication")
                    .GetOrAdd("basicAuthentication")
                    .SetAttributeValue("enabled", enabled);
            });
    }

    public static void EnsureSection(this IISDeploymentParameters parameters, string name, params string[] path)
    {
        parameters.ServerConfigActionList.Add(
            (config, _) =>
            {

                var element = config
                    .RequiredElement("configSections");

                foreach (var s in path)
                {
                    element = element.GetOrAdd("sectionGroup", "name", s);
                }

                element.GetOrAdd("section", "name", "applicationInitialization")
                   .SetAttributeValue("overrideModeDefault", "Allow");
            });
    }

    public static void EnableLogging(this IISDeploymentParameters deploymentParameters, string path)
    {
        deploymentParameters.WebConfigActionList.Add(
            WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogEnabled", "true"));

        deploymentParameters.WebConfigActionList.Add(
            WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogFile", Path.Combine(path, "std")));
    }

    public static void EnableFreb(this IISDeploymentParameters deploymentParameters, string verbosity, string folderPath)
    {
        if (!deploymentParameters.PublishApplicationBeforeDeployment)
        {
            throw new InvalidOperationException("Testing freb requires site to be published.");
        }

        deploymentParameters.EnableModule("FailedRequestsTracingModule", "%IIS_BIN%\\iisfreb.dll");

        // Set the TraceFailedRequestsSection to listend to ANCM events
        deploymentParameters.ServerConfigActionList.Add(
            (element, _) =>
            {
                var webServerElement = element
                        .RequiredElement("system.webServer");

                var addElement = webServerElement
                    .GetOrAdd("tracing")
                    .GetOrAdd("traceFailedRequests")
                    .GetOrAdd("add");

                addElement.SetAttributeValue("path", "*");

                addElement.GetOrAdd("failureDefinitions")
                    .SetAttributeValue("statusCodes", "200-999");

                var traceAreasElement = addElement
                    .GetOrAdd("traceAreas");
                var innerAddElement = traceAreasElement.GetOrAdd("add", "provider", "WWW Server");

                innerAddElement.SetAttributeValue("areas", "ANCM");
                innerAddElement.SetAttributeValue("verbosity", verbosity);
            });

        // Set the ANCM traceProviderDefinition to 65536
        deploymentParameters.ServerConfigActionList.Add(
            (element, _) =>
            {
                var webServerElement = element
                        .RequiredElement("system.webServer");

                var traceProviderDefinitionsElement = webServerElement
                    .GetOrAdd("tracing")
                    .GetOrAdd("traceProviderDefinitions");

                var innerAddElement = traceProviderDefinitionsElement.GetOrAdd("add", "name", "WWW Server");

                innerAddElement.SetAttributeValue("name", "WWW Server");
                innerAddElement.SetAttributeValue("guid", "{3a2a4e84-4c21-4981-ae10-3fda0d9b0f83}");

                var areasElement = innerAddElement.GetOrAdd("areas");
                var iae = areasElement.GetOrAdd("add", "name", "ANCM");

                iae.SetAttributeValue("value", "65536");
            });

        // Set the freb directory to the published app directory.
        deploymentParameters.ServerConfigActionList.Add(
            (element, contentRoot) =>
            {
                var traceFailedRequestsElement = element
                    .RequiredElement("system.applicationHost")
                    .Element("sites")
                    .Element("siteDefaults")
                    .Element("traceFailedRequestsLogging");
                traceFailedRequestsElement.SetAttributeValue("directory", folderPath);
                traceFailedRequestsElement.SetAttributeValue("enabled", "true");
                traceFailedRequestsElement.SetAttributeValue("maxLogFileSizeKB", "1024");
            });
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
            (element, _) =>
            {
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
