// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class InternalKestrelServerOptions
    {
        // This will likely be replace with transport-specific configuration.
        // https://github.com/aspnet/KestrelHttpServer/issues/828
        public bool ThreadPoolDispatching { get; set; } = true;
    }
}
