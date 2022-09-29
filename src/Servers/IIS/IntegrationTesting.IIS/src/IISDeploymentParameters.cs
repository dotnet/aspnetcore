// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

public class IISDeploymentParameters : DeploymentParameters
{
    public IISDeploymentParameters() : base()
    {
    }

    public IISDeploymentParameters(TestVariant variant)
        : base(variant)
    {
    }

    public IISDeploymentParameters(
        string applicationPath,
        ServerType serverType,
        RuntimeFlavor runtimeFlavor,
        RuntimeArchitecture runtimeArchitecture)
        : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
    {
    }

    public IISDeploymentParameters(DeploymentParameters parameters)
        : base(parameters)
    {
        if (parameters is IISDeploymentParameters tempParameters)
        {
            WebConfigActionList = tempParameters.WebConfigActionList;
            ServerConfigActionList = tempParameters.ServerConfigActionList;
            WebConfigBasedEnvironmentVariables = tempParameters.WebConfigBasedEnvironmentVariables;
            HandlerSettings = tempParameters.HandlerSettings;
        }
    }

    public IList<Action<XElement, string>> WebConfigActionList { get; } = new List<Action<XElement, string>>();

    public IList<Action<XElement, string>> ServerConfigActionList { get; } = new List<Action<XElement, string>>();

    public IDictionary<string, string> WebConfigBasedEnvironmentVariables { get; set; } = new Dictionary<string, string>();

    public IDictionary<string, string> HandlerSettings { get; set; } = new Dictionary<string, string>();
}
