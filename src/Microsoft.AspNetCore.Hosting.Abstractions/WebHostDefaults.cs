// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostDefaults
    {
        public static readonly string ApplicationKey = "application";
        public static readonly string DetailedErrorsKey = "detailedErrors";
        public static readonly string EnvironmentKey = "environment";
        public static readonly string ServerKey = "server";
        public static readonly string WebRootKey = "webroot";
        public static readonly string CaptureStartupErrorsKey = "captureStartupErrors";
        public static readonly string ServerUrlsKey = "server.urls";
        public static readonly string ApplicationBaseKey = "applicationBase";

        public static readonly string HostingJsonFile = "hosting.json";
        public static readonly string EnvironmentVariablesPrefix = "ASPNET_";
    }
}
