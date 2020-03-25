// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class IISExpressAncmSchema
    {
        public static bool SupportsInProcessHosting { get; }
        public static string SkipReason { get; }

        static IISExpressAncmSchema()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SkipReason = "IIS Express tests can only be run on Windows";
                return;
            }

            var ancmConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "IIS Express", "config", "schema", "aspnetcore_schema.xml");

            if (!File.Exists(ancmConfigPath))
            {
                SkipReason = "IIS Express is not installed.";
                return;
            }

            XDocument ancmConfig;

            try
            {
                ancmConfig = XDocument.Load(ancmConfigPath);
            }
            catch
            {
                SkipReason = "Could not read ANCM schema configuration";
                return;
            }

            SupportsInProcessHosting = ancmConfig
                .Root
                .Descendants("attribute")
                .Any(n => "hostingModel".Equals(n.Attribute("name")?.Value, StringComparison.Ordinal));

            SkipReason = SupportsInProcessHosting ? null : "IIS Express must be upgraded to support in-process hosting.";
        }
    }
}