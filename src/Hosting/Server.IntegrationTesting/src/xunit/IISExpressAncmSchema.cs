// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class IISExpressAncmSchema
{
    public static bool SupportsInProcessHosting { get; }
    public static string SkipReason { get; }

    static IISExpressAncmSchema()
    {
        if (!OperatingSystem.IsWindows())
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
