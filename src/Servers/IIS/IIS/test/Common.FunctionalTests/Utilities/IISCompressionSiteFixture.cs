// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class IISCompressionSiteFixture : IISTestSiteFixture
    {
        public IISCompressionSiteFixture() : base(Configure)
        {
        }

        private static void Configure(IISDeploymentParameters deploymentParameters)
        {
            // Enable dynamic compression
            deploymentParameters.ServerConfigActionList.Add(
                (element, _) => {
                    var webServerElement = element
                        .RequiredElement("system.webServer");

                    webServerElement
                        .GetOrAdd("urlCompression")
                        .SetAttributeValue("doDynamicCompression", "true");

                    webServerElement
                        .GetOrAdd("httpCompression")
                        .GetOrAdd("dynamicTypes")
                        .GetOrAdd("add", "mimeType", "text/*")
                        .SetAttributeValue("enabled", "true");

                });

            deploymentParameters.EnableModule("DynamicCompressionModule", "%IIS_BIN%\\compdyn.dll");
        }
    }
}
