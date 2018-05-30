// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit.Abstractions;

namespace IISIntegration.FunctionalTests.Utilities
{
    public class IISFunctionalTestBase : FunctionalTestsBase
    {
        public IISFunctionalTestBase(ITestOutputHelper output = null) : base(output)
        {
        }

        protected string GetServerConfig(Action<XElement> transform)
        {
            var doc = XDocument.Load("AppHostConfig/Http.config");
            transform?.Invoke(doc.Root);
            return doc.ToString();
        }

        protected string GetHttpsServerConfig()
        {
            return GetServerConfig(
                element => {
                    element.Descendants("binding")
                        .Single()
                        .SetAttributeValue("protocol", "https");

                    element.Descendants("access")
                        .Single()
                        .SetAttributeValue("sslFlags", "Ssl, SslNegotiateCert");
                });
        }
    }
}
