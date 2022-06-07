// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore;

public static class TestData
{
    public static List<string> ListedSharedFxAssemblies;

    public static SortedDictionary<string, string> ListedTargetingPackAssemblies;

    static TestData()
    {
        ListedSharedFxAssemblies = new List<string>()
            {
                "aspnetcorev2_inprocess",
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.Antiforgery",
                "Microsoft.AspNetCore.Authentication",
                "Microsoft.AspNetCore.Authentication.Abstractions",
                "Microsoft.AspNetCore.Authentication.Cookies",
                "Microsoft.AspNetCore.Authentication.Core",
                "Microsoft.AspNetCore.Authentication.OAuth",
                "Microsoft.AspNetCore.Authorization",
                "Microsoft.AspNetCore.Authorization.Policy",
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Authorization",
                "Microsoft.AspNetCore.Components.Forms",
                "Microsoft.AspNetCore.Components.Server",
                "Microsoft.AspNetCore.Components.Web",
                "Microsoft.AspNetCore.Connections.Abstractions",
                "Microsoft.AspNetCore.CookiePolicy",
                "Microsoft.AspNetCore.Cors",
                "Microsoft.AspNetCore.Cryptography.Internal",
                "Microsoft.AspNetCore.Cryptography.KeyDerivation",
                "Microsoft.AspNetCore.DataProtection",
                "Microsoft.AspNetCore.DataProtection.Abstractions",
                "Microsoft.AspNetCore.DataProtection.Extensions",
                "Microsoft.AspNetCore.Diagnostics",
                "Microsoft.AspNetCore.Diagnostics.Abstractions",
                "Microsoft.AspNetCore.Diagnostics.HealthChecks",
                "Microsoft.AspNetCore.HostFiltering",
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Hosting.Abstractions",
                "Microsoft.AspNetCore.Hosting.Server.Abstractions",
                "Microsoft.AspNetCore.Html.Abstractions",
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Http.Abstractions",
                "Microsoft.AspNetCore.Http.Connections",
                "Microsoft.AspNetCore.Http.Connections.Common",
                "Microsoft.AspNetCore.Http.Extensions",
                "Microsoft.AspNetCore.Http.Features",
                "Microsoft.AspNetCore.Http.Results",
                "Microsoft.AspNetCore.HttpLogging",
                "Microsoft.AspNetCore.HttpOverrides",
                "Microsoft.AspNetCore.HttpsPolicy",
                "Microsoft.AspNetCore.Identity",
                "Microsoft.AspNetCore.Localization",
                "Microsoft.AspNetCore.Localization.Routing",
                "Microsoft.AspNetCore.Metadata",
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.AspNetCore.Mvc.Abstractions",
                "Microsoft.AspNetCore.Mvc.ApiExplorer",
                "Microsoft.AspNetCore.Mvc.Core",
                "Microsoft.AspNetCore.Mvc.Cors",
                "Microsoft.AspNetCore.Mvc.DataAnnotations",
                "Microsoft.AspNetCore.Mvc.Formatters.Json",
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
                "Microsoft.AspNetCore.Mvc.Localization",
                "Microsoft.AspNetCore.Mvc.Razor",
                "Microsoft.AspNetCore.Mvc.RazorPages",
                "Microsoft.AspNetCore.Mvc.TagHelpers",
                "Microsoft.AspNetCore.Mvc.ViewFeatures",
                "Microsoft.AspNetCore.Razor",
                "Microsoft.AspNetCore.Razor.Runtime",
                "Microsoft.AspNetCore.RequestDecompression",
                "Microsoft.AspNetCore.ResponseCaching",
                "Microsoft.AspNetCore.ResponseCaching.Abstractions",
                "Microsoft.AspNetCore.ResponseCompression",
                "Microsoft.AspNetCore.Rewrite",
                "Microsoft.AspNetCore.Routing",
                "Microsoft.AspNetCore.Routing.Abstractions",
                "Microsoft.AspNetCore.Server.HttpSys",
                "Microsoft.AspNetCore.Server.IIS",
                "Microsoft.AspNetCore.Server.IISIntegration",
                "Microsoft.AspNetCore.Server.Kestrel",
                "Microsoft.AspNetCore.Server.Kestrel.Core",
                "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic",
                "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets",
                "Microsoft.AspNetCore.Session",
                "Microsoft.AspNetCore.SignalR",
                "Microsoft.AspNetCore.SignalR.Common",
                "Microsoft.AspNetCore.SignalR.Core",
                "Microsoft.AspNetCore.SignalR.Protocols.Json",
                "Microsoft.AspNetCore.StaticFiles",
                "Microsoft.AspNetCore.WebSockets",
                "Microsoft.AspNetCore.WebUtilities",
                "Microsoft.Extensions.Caching.Abstractions",
                "Microsoft.Extensions.Caching.Memory",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.Configuration.Abstractions",
                "Microsoft.Extensions.Configuration.Binder",
                "Microsoft.Extensions.Configuration.CommandLine",
                "Microsoft.Extensions.Configuration.EnvironmentVariables",
                "Microsoft.Extensions.Configuration.FileExtensions",
                "Microsoft.Extensions.Configuration.Ini",
                "Microsoft.Extensions.Configuration.Json",
                "Microsoft.Extensions.Configuration.KeyPerFile",
                "Microsoft.Extensions.Configuration.UserSecrets",
                "Microsoft.Extensions.Configuration.Xml",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Extensions.Diagnostics.HealthChecks",
                "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions",
                "Microsoft.Extensions.FileProviders.Abstractions",
                "Microsoft.Extensions.FileProviders.Composite",
                "Microsoft.Extensions.FileProviders.Embedded",
                "Microsoft.Extensions.FileProviders.Physical",
                "Microsoft.Extensions.FileSystemGlobbing",
                "Microsoft.Extensions.Features",
                "Microsoft.Extensions.Hosting",
                "Microsoft.Extensions.Hosting.Abstractions",
                "Microsoft.Extensions.Http",
                "Microsoft.Extensions.Identity.Core",
                "Microsoft.Extensions.Identity.Stores",
                "Microsoft.Extensions.Localization",
                "Microsoft.Extensions.Localization.Abstractions",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Logging.Configuration",
                "Microsoft.Extensions.Logging.Console",
                "Microsoft.Extensions.Logging.Debug",
                "Microsoft.Extensions.Logging.EventLog",
                "Microsoft.Extensions.Logging.EventSource",
                "Microsoft.Extensions.Logging.TraceSource",
                "Microsoft.Extensions.ObjectPool",
                "Microsoft.Extensions.Options",
                "Microsoft.Extensions.Options.ConfigurationExtensions",
                "Microsoft.Extensions.Options.DataAnnotations",
                "Microsoft.Extensions.Primitives",
                "Microsoft.Extensions.WebEncoders",
                "Microsoft.JSInterop",
                "Microsoft.Net.Http.Headers",
                "System.Diagnostics.EventLog",
                "System.Diagnostics.EventLog.Messages",
                "System.IO.Pipelines",
                "System.Security.Cryptography.Pkcs",
                "System.Security.Cryptography.Xml",
            };

        ListedTargetingPackAssemblies = new SortedDictionary<string, string>
            {
                { "Microsoft.AspNetCore", "7.0.0.0" },
                { "Microsoft.AspNetCore.Antiforgery", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authentication", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Cookies", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Core", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.OAuth", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authorization", "7.0.0.0" },
                { "Microsoft.AspNetCore.Authorization.Policy", "7.0.0.0" },
                { "Microsoft.AspNetCore.Components", "7.0.0.0" },
                { "Microsoft.AspNetCore.Components.Authorization", "7.0.0.0" },
                { "Microsoft.AspNetCore.Components.Forms", "7.0.0.0" },
                { "Microsoft.AspNetCore.Components.Server", "7.0.0.0" },
                { "Microsoft.AspNetCore.Components.Web", "7.0.0.0" },
                { "Microsoft.AspNetCore.Connections.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.CookiePolicy", "7.0.0.0" },
                { "Microsoft.AspNetCore.Cors", "7.0.0.0" },
                { "Microsoft.AspNetCore.Cryptography.Internal", "7.0.0.0" },
                { "Microsoft.AspNetCore.Cryptography.KeyDerivation", "7.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection", "7.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection.Extensions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics", "7.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics.HealthChecks", "7.0.0.0" },
                { "Microsoft.AspNetCore.HostFiltering", "7.0.0.0" },
                { "Microsoft.AspNetCore.Hosting", "7.0.0.0" },
                { "Microsoft.AspNetCore.Hosting.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Hosting.Server.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Html.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Connections", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Connections.Common", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Extensions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Features", "7.0.0.0" },
                { "Microsoft.AspNetCore.Http.Results", "7.0.0.0" },
                { "Microsoft.AspNetCore.HttpLogging", "7.0.0.0" },
                { "Microsoft.AspNetCore.HttpOverrides", "7.0.0.0" },
                { "Microsoft.AspNetCore.HttpsPolicy", "7.0.0.0" },
                { "Microsoft.AspNetCore.Identity", "7.0.0.0" },
                { "Microsoft.AspNetCore.Localization", "7.0.0.0" },
                { "Microsoft.AspNetCore.Localization.Routing", "7.0.0.0" },
                { "Microsoft.AspNetCore.Metadata", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.ApiExplorer", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Core", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Cors", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.DataAnnotations", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Json", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Xml", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Localization", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Razor", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.RazorPages", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.TagHelpers", "7.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.ViewFeatures", "7.0.0.0" },
                { "Microsoft.AspNetCore.Razor", "7.0.0.0" },
                { "Microsoft.AspNetCore.Razor.Runtime", "7.0.0.0" },
                { "Microsoft.AspNetCore.RequestDecompression", "7.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCaching", "7.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCaching.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCompression", "7.0.0.0" },
                { "Microsoft.AspNetCore.Rewrite", "7.0.0.0" },
                { "Microsoft.AspNetCore.Routing", "7.0.0.0" },
                { "Microsoft.AspNetCore.Routing.Abstractions", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.HttpSys", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.IIS", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.IISIntegration", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel.Core", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic", "7.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets", "7.0.0.0" },
                { "Microsoft.AspNetCore.Session", "7.0.0.0" },
                { "Microsoft.AspNetCore.SignalR", "7.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Common", "7.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Core", "7.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Protocols.Json", "7.0.0.0" },
                { "Microsoft.AspNetCore.StaticFiles", "7.0.0.0" },
                { "Microsoft.AspNetCore.WebSockets", "7.0.0.0" },
                { "Microsoft.AspNetCore.WebUtilities", "7.0.0.0" },
                { "Microsoft.Extensions.Caching.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Caching.Memory", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.Binder", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.CommandLine", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.EnvironmentVariables", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.FileExtensions", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.Ini", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.Json", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.KeyPerFile", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.UserSecrets", "7.0.0.0" },
                { "Microsoft.Extensions.Configuration.Xml", "7.0.0.0" },
                { "Microsoft.Extensions.DependencyInjection", "7.0.0.0" },
                { "Microsoft.Extensions.DependencyInjection.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks", "7.0.0.0" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Composite", "7.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Embedded", "7.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Physical", "7.0.0.0" },
                { "Microsoft.Extensions.FileSystemGlobbing", "7.0.0.0" },
                { "Microsoft.Extensions.Features", "7.0.0.0" },
                { "Microsoft.Extensions.Hosting", "7.0.0.0" },
                { "Microsoft.Extensions.Hosting.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Http", "7.0.0.0" },
                { "Microsoft.Extensions.Identity.Core", "7.0.0.0" },
                { "Microsoft.Extensions.Identity.Stores", "7.0.0.0" },
                { "Microsoft.Extensions.Localization", "7.0.0.0" },
                { "Microsoft.Extensions.Localization.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Logging", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.Abstractions", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.Configuration", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.Console", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.Debug", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.EventLog", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.EventSource", "7.0.0.0" },
                { "Microsoft.Extensions.Logging.TraceSource", "7.0.0.0" },
                { "Microsoft.Extensions.ObjectPool", "7.0.0.0" },
                { "Microsoft.Extensions.Options", "7.0.0.0" },
                { "Microsoft.Extensions.Options.ConfigurationExtensions", "7.0.0.0" },
                { "Microsoft.Extensions.Options.DataAnnotations", "7.0.0.0" },
                { "Microsoft.Extensions.Primitives", "7.0.0.0" },
                { "Microsoft.Extensions.WebEncoders", "7.0.0.0" },
                { "Microsoft.JSInterop", "7.0.0.0" },
                { "Microsoft.Net.Http.Headers", "7.0.0.0" },
                { "System.Diagnostics.EventLog", "7.0.0.0" },
                { "System.IO.Pipelines", "7.0.0.0" },
                { "System.Security.Cryptography.Xml", "7.0.0.0" },
            };

        if (!VerifyAncmBinary())
        {
            ListedSharedFxAssemblies.Remove("aspnetcorev2_inprocess");
        }
    }

    public static string GetSharedFxVersion() => GetTestDataValue("SharedFxVersion");

    public static string GetDefaultNetCoreTargetFramework() => GetTestDataValue("DefaultNetCoreTargetFramework");

    public static string GetMicrosoftNETCoreAppPackageVersion() => GetTestDataValue("MicrosoftNETCoreAppRuntimeVersion");

    public static string GetReferencePackSharedFxVersion() => GetTestDataValue("ReferencePackSharedFxVersion");

    public static string GetRepositoryCommit() => GetTestDataValue("RepositoryCommit");

    public static string GetSharedFxRuntimeIdentifier() => GetTestDataValue("TargetRuntimeIdentifier");

    public static string GetSharedFrameworkBinariesFromRepo() => GetTestDataValue("SharedFrameworkBinariesFromRepo");

    public static string GetSharedFxDependencies() => GetTestDataValue("SharedFxDependencies");

    public static string GetTargetingPackDependencies() => GetTestDataValue("TargetingPackDependencies");

    public static string GetRuntimeTargetingPackDependencies() => GetTestDataValue("RuntimeTargetingPackDependencies");

    public static string GetAspNetCoreTargetingPackDependencies() => GetTestDataValue("AspNetCoreTargetingPackDependencies");

    public static string GetPackagesFolder() => GetTestDataValue("PackagesFolder");

    public static bool VerifyAncmBinary() => string.Equals(GetTestDataValue("VerifyAncmBinary"), "true", StringComparison.OrdinalIgnoreCase);

    private static string GetTestDataValue(string key)
         => typeof(TestData).Assembly.GetCustomAttributes<TestDataAttribute>().Single(d => d.Key == key).Value;
}
