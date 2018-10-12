// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Flags]
    public enum IISCapability
    {
        None = 0,
        Websockets = 1,
        WindowsAuthentication = 2,
        PoolEnvironmentVariables = 4,
        ShutdownToken = 8,
        DynamicCompression = 16,
        ApplicationInitialization = 32,
        TracingModule = 64,
        FailedRequestTracingModule = 128,
        BasicAuthentication = 256
    }
}
