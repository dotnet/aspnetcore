// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresIISAttribute : Attribute, ITestCondition
    {
        private static readonly bool _isMet;
        public static readonly string _skipReason;

        public bool IsMet => _isMet;
        public string SkipReason => _skipReason;

        static RequiresIISAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _skipReason = "IIS tests can only be run on Windows";
                return;
            }

            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                _skipReason += "The current console is not running as admin.";
                return;
            }

            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "inetsrv", "w3wp.exe")))
            {
                _skipReason += "The machine does not have IIS installed.";
                return;
            }

            var ancmConfigPath = Path.Combine(Environment.SystemDirectory, "inetsrv", "config", "schema", "aspnetcore_schema_v2.xml");

            if (!File.Exists(ancmConfigPath))
            {
                _skipReason = "IIS Schema is not installed.";
                return;
            }

            XDocument ancmConfig;

            try
            {
                ancmConfig = XDocument.Load(ancmConfigPath);
            }
            catch
            {
                _skipReason = "Could not read ANCM schema configuration";
                return;
            }

            _isMet = ancmConfig
                .Root
                .Descendants("attribute")
                .Any(n => "hostingModel".Equals(n.Attribute("name")?.Value, StringComparison.Ordinal));

            _skipReason = _isMet ? null : "IIS schema needs to be upgraded to support ANCM.";
        }
    }
}
