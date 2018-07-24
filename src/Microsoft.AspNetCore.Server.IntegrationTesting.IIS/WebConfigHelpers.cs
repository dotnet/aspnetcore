// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public static class WebConfigHelpers
    {
        public static Action<XElement> AddOrModifyAspNetCoreSection(string key, string value)
            => AddAction(key, value, section: "aspNetCore");

        public static Action<XElement> AddAction(string key, string value, string section)
        {
            return (element) =>
            {
                element.Descendants(section).SingleOrDefault().SetAttributeValue(key, value);
            };
        }

        public static Action<XElement> AddOrModifyHandlerSection(string key, string value)
        {
            return element =>
            {
                element.Descendants("handlers")
                        .FirstOrDefault()
                        .Descendants("add")
                        .FirstOrDefault()
                        .SetAttributeValue(key, value);
            };
        }
    }
}
