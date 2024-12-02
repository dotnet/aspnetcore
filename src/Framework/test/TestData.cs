// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore;

public static class TestData
{
    public static List<string> ListedSharedFxAssemblies;

    public static List<string> ListedTargetingPackAssemblies;

    static TestData()
    {
        ListedSharedFxAssemblies = new List<string>()
            {
                "aspnetcorev2_inprocess",
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.Antiforgery",
                "Microsoft.AspNetCore.Authentication",
                "Microsoft.AspNetCore.Authentication.Abstractions",
                "Microsoft.AspNetCore.Authentication.BearerToken",
                "Microsoft.AspNetCore.Authentication.Cookies",
                "Microsoft.AspNetCore.Authentication.Core",
                "Microsoft.AspNetCore.Authentication.OAuth",
                "Microsoft.AspNetCore.Authorization",
                "Microsoft.AspNetCore.Authorization.Policy",
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Authorization",
                "Microsoft.AspNetCore.Components.Endpoints",
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
                "Microsoft.AspNetCore.OutputCaching",
                "Microsoft.AspNetCore.RateLimiting",
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
                "Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes",
                "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets",
                "Microsoft.AspNetCore.Session",
                "Microsoft.AspNetCore.SignalR",
                "Microsoft.AspNetCore.SignalR.Common",
                "Microsoft.AspNetCore.SignalR.Core",
                "Microsoft.AspNetCore.SignalR.Protocols.Json",
                "Microsoft.AspNetCore.StaticFiles",
                "Microsoft.AspNetCore.StaticAssets",
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
                "Microsoft.Extensions.Diagnostics",
                "Microsoft.Extensions.Diagnostics.Abstractions",
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
                "System.Security.Cryptography.Pkcs",
                "System.Security.Cryptography.Xml",
                "System.Threading.RateLimiting",
            };

        // System.Diagnostics.EventLog.Messages is only present in the Windows build.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !SkipOnHelixAttribute.OnHelix()) // Helix tests always run against the Windows assets (even on non-Windows)
        {
            ListedSharedFxAssemblies.Remove("System.Diagnostics.EventLog.Messages");
        }

        ListedTargetingPackAssemblies = new List<string>
            {
                { "Microsoft.AspNetCore.Antiforgery" },
                { "Microsoft.AspNetCore.Authentication.Abstractions" },
                { "Microsoft.AspNetCore.Authentication.BearerToken" },
                { "Microsoft.AspNetCore.Authentication.Cookies" },
                { "Microsoft.AspNetCore.Authentication.Core" },
                { "Microsoft.AspNetCore.Authentication.OAuth" },
                { "Microsoft.AspNetCore.Authentication" },
                { "Microsoft.AspNetCore.Authorization.Policy" },
                { "Microsoft.AspNetCore.Authorization" },
                { "Microsoft.AspNetCore.Components.Authorization" },
                { "Microsoft.AspNetCore.Components.Forms" },
                { "Microsoft.AspNetCore.Components.Endpoints" },
                { "Microsoft.AspNetCore.Components.Server" },
                { "Microsoft.AspNetCore.Components.Web" },
                { "Microsoft.AspNetCore.Components" },
                { "Microsoft.AspNetCore.Connections.Abstractions" },
                { "Microsoft.AspNetCore.CookiePolicy" },
                { "Microsoft.AspNetCore.Cors" },
                { "Microsoft.AspNetCore.Cryptography.Internal" },
                { "Microsoft.AspNetCore.Cryptography.KeyDerivation" },
                { "Microsoft.AspNetCore.DataProtection.Abstractions" },
                { "Microsoft.AspNetCore.DataProtection.Extensions" },
                { "Microsoft.AspNetCore.DataProtection" },
                { "Microsoft.AspNetCore.Diagnostics.Abstractions" },
                { "Microsoft.AspNetCore.Diagnostics.HealthChecks" },
                { "Microsoft.AspNetCore.Diagnostics" },
                { "Microsoft.AspNetCore.HostFiltering" },
                { "Microsoft.AspNetCore.Hosting.Abstractions" },
                { "Microsoft.AspNetCore.Hosting.Server.Abstractions" },
                { "Microsoft.AspNetCore.Hosting" },
                { "Microsoft.AspNetCore.Html.Abstractions" },
                { "Microsoft.AspNetCore.Http.Abstractions" },
                { "Microsoft.AspNetCore.Http.Connections.Common" },
                { "Microsoft.AspNetCore.Http.Connections" },
                { "Microsoft.AspNetCore.Http.Extensions" },
                { "Microsoft.AspNetCore.Http.Features" },
                { "Microsoft.AspNetCore.Http.Results" },
                { "Microsoft.AspNetCore.Http" },
                { "Microsoft.AspNetCore.HttpLogging" },
                { "Microsoft.AspNetCore.HttpOverrides" },
                { "Microsoft.AspNetCore.HttpsPolicy" },
                { "Microsoft.AspNetCore.Identity" },
                { "Microsoft.AspNetCore.Localization.Routing" },
                { "Microsoft.AspNetCore.Localization" },
                { "Microsoft.AspNetCore.Metadata" },
                { "Microsoft.AspNetCore.Mvc.Abstractions" },
                { "Microsoft.AspNetCore.Mvc.ApiExplorer" },
                { "Microsoft.AspNetCore.Mvc.Core" },
                { "Microsoft.AspNetCore.Mvc.Cors" },
                { "Microsoft.AspNetCore.Mvc.DataAnnotations" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Json" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Xml" },
                { "Microsoft.AspNetCore.Mvc.Localization" },
                { "Microsoft.AspNetCore.Mvc.Razor" },
                { "Microsoft.AspNetCore.Mvc.RazorPages" },
                { "Microsoft.AspNetCore.Mvc.TagHelpers" },
                { "Microsoft.AspNetCore.Mvc.ViewFeatures" },
                { "Microsoft.AspNetCore.Mvc" },
                { "Microsoft.AspNetCore.OutputCaching" },
                { "Microsoft.AspNetCore.RateLimiting" },
                { "Microsoft.AspNetCore.Razor.Runtime" },
                { "Microsoft.AspNetCore.Razor" },
                { "Microsoft.AspNetCore.RequestDecompression" },
                { "Microsoft.AspNetCore.ResponseCaching.Abstractions" },
                { "Microsoft.AspNetCore.ResponseCaching" },
                { "Microsoft.AspNetCore.ResponseCompression" },
                { "Microsoft.AspNetCore.Rewrite" },
                { "Microsoft.AspNetCore.Routing.Abstractions" },
                { "Microsoft.AspNetCore.Routing" },
                { "Microsoft.AspNetCore.Server.HttpSys" },
                { "Microsoft.AspNetCore.Server.IIS" },
                { "Microsoft.AspNetCore.Server.IISIntegration" },
                { "Microsoft.AspNetCore.Server.Kestrel.Core" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes" },
                { "Microsoft.AspNetCore.Server.Kestrel" },
                { "Microsoft.AspNetCore.Session" },
                { "Microsoft.AspNetCore.SignalR.Common" },
                { "Microsoft.AspNetCore.SignalR.Core" },
                { "Microsoft.AspNetCore.SignalR.Protocols.Json" },
                { "Microsoft.AspNetCore.SignalR" },
                { "Microsoft.AspNetCore.StaticFiles" },
                { "Microsoft.AspNetCore.StaticAssets" },
                { "Microsoft.AspNetCore.WebSockets" },
                { "Microsoft.AspNetCore.WebUtilities" },
                { "Microsoft.AspNetCore" },
                { "Microsoft.Extensions.Caching.Abstractions" },
                { "Microsoft.Extensions.Caching.Memory" },
                { "Microsoft.Extensions.Configuration.Abstractions" },
                { "Microsoft.Extensions.Configuration.Binder" },
                { "Microsoft.Extensions.Configuration.CommandLine" },
                { "Microsoft.Extensions.Configuration.EnvironmentVariables" },
                { "Microsoft.Extensions.Configuration.FileExtensions" },
                { "Microsoft.Extensions.Configuration.Ini" },
                { "Microsoft.Extensions.Configuration.Json" },
                { "Microsoft.Extensions.Configuration.KeyPerFile" },
                { "Microsoft.Extensions.Configuration.UserSecrets" },
                { "Microsoft.Extensions.Configuration.Xml" },
                { "Microsoft.Extensions.Configuration" },
                { "Microsoft.Extensions.DependencyInjection.Abstractions" },
                { "Microsoft.Extensions.DependencyInjection" },
                { "Microsoft.Extensions.Diagnostics" },
                { "Microsoft.Extensions.Diagnostics.Abstractions" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks" },
                { "Microsoft.Extensions.Features" },
                { "Microsoft.Extensions.FileProviders.Abstractions" },
                { "Microsoft.Extensions.FileProviders.Composite" },
                { "Microsoft.Extensions.FileProviders.Embedded" },
                { "Microsoft.Extensions.FileProviders.Physical" },
                { "Microsoft.Extensions.FileSystemGlobbing" },
                { "Microsoft.Extensions.Hosting.Abstractions" },
                { "Microsoft.Extensions.Hosting" },
                { "Microsoft.Extensions.Http" },
                { "Microsoft.Extensions.Identity.Core" },
                { "Microsoft.Extensions.Identity.Stores" },
                { "Microsoft.Extensions.Localization.Abstractions" },
                { "Microsoft.Extensions.Localization" },
                { "Microsoft.Extensions.Logging.Abstractions" },
                { "Microsoft.Extensions.Logging.Configuration" },
                { "Microsoft.Extensions.Logging.Console" },
                { "Microsoft.Extensions.Logging.Debug" },
                { "Microsoft.Extensions.Logging.EventLog" },
                { "Microsoft.Extensions.Logging.EventSource" },
                { "Microsoft.Extensions.Logging.TraceSource" },
                { "Microsoft.Extensions.Logging" },
                { "Microsoft.Extensions.ObjectPool" },
                { "Microsoft.Extensions.Options.ConfigurationExtensions" },
                { "Microsoft.Extensions.Options.DataAnnotations" },
                { "Microsoft.Extensions.Options" },
                { "Microsoft.Extensions.Primitives" },
                { "Microsoft.Extensions.WebEncoders" },
                { "Microsoft.JSInterop" },
                { "Microsoft.Net.Http.Headers" },
                { "System.Diagnostics.EventLog" },
                { "System.Security.Cryptography.Xml" },
                { "System.Threading.RateLimiting" },
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

    public static string GetPackageLayoutRoot() => GetTestDataValue("PackageLayoutRoot");

    public static bool VerifyAncmBinary() => string.Equals(GetTestDataValue("VerifyAncmBinary"), "true", StringComparison.OrdinalIgnoreCase);

    public static bool VerifyPackageAssemblyVersions() => string.Equals(GetTestDataValue("VerifyPackageAssemblyVersions"), "true", StringComparison.OrdinalIgnoreCase);

    private static string GetTestDataValue(string key)
         => typeof(TestData).Assembly.GetCustomAttributes<TestDataAttribute>().Single(d => d.Key == key).Value;
}
