// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SpaProxy
{
    internal class SpaDevelopmentServerOptions
        {
            public string ServerUrl { get; set; } = "";

            public string LaunchCommand { get; set; } = "";

            public int MaxTimeoutInSeconds { get; set; }

            public TimeSpan MaxTimeout => TimeSpan.FromSeconds(MaxTimeoutInSeconds);

            public string WorkingDirectory { get; set; } = "";
        }
}
