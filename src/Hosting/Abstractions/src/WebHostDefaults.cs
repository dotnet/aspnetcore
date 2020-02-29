// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostDefaults
    {
        public static readonly string ApplicationKey = "applicationName";
        public static readonly string StartupAssemblyKey = "startupAssembly";
        public static readonly string HostingStartupAssembliesKey = "hostingStartupAssemblies";
        public static readonly string HostingStartupExcludeAssembliesKey = "hostingStartupExcludeAssemblies";

        public static readonly string DetailedErrorsKey = "detailedErrors";
        public static readonly string EnvironmentKey = "environment";
        public static readonly string WebRootKey = "webroot";
        public static readonly string CaptureStartupErrorsKey = "captureStartupErrors";
        public static readonly string ServerUrlsKey = "urls";
        public static readonly string ContentRootKey = "contentRoot";
        public static readonly string PreferHostingUrlsKey = "preferHostingUrls";
        public static readonly string PreventHostingStartupKey = "preventHostingStartup";
        public static readonly string SuppressStatusMessagesKey = "suppressStatusMessages";

        public static readonly string ShutdownTimeoutKey = "shutdownTimeoutSeconds";
        public static readonly string StaticWebAssetsKey = "staticWebAssets";
    }
}
