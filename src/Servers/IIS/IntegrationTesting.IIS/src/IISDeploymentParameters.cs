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
            if (parameters is IISDeploymentParameters)
            {
                var tempParameters = (IISDeploymentParameters)parameters;
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
}
