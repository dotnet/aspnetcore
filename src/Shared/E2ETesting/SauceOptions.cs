// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.E2ETesting
{
    public class SauceOptions
    {
        public string Username { get; set; }

        public string AccessKey { get; set; }

        public string TunnelIdentifier { get; set; }

        public string HostName { get; set; }

        public string TestName { get; set; }

        public bool IsRealDevice { get; set; }

        public string PlatformName { get; set; }

        public string PlatformVersion { get; set; }

        public string BrowserName { get; set; }

        public string BrowserVersion { get; set; }

        public string DeviceName { get; set; }

        public string DeviceOrientation { get; set; }

        public string AppiumVersion { get; set; }

        public string SeleniumVersion { get; set; }
    }
}
