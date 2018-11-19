// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ApplicationModelDetection
{
    public class AppModelDetectionResult
    {
        public RuntimeFramework? Framework { get; set; }
        public string FrameworkVersion { get; set; }
        public string AspNetCoreVersion { get; set; }
    }
}