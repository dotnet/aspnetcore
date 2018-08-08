// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public static class WebConfigHelpers
    {
        public static Action<XElement, string> AddOrModifyAspNetCoreSection(string key, string value)
        {
            return (element, _) => {
                element
                    .GetOrAdd("system.webServer")
                    .GetOrAdd("aspNetCore")
                    .SetAttributeValue(key, value);
            };
        }

        public static Action<XElement, string> AddOrModifyHandlerSection(string key, string value)
        {
            return (element, _) =>
            {
                element
                    .GetOrAdd("system.webServer")
                    .GetOrAdd("handlers")
                    .GetOrAdd("add")
                    .SetAttributeValue(key, value);
            };
        }
    }
}
