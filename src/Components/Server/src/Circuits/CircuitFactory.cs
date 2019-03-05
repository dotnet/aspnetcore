// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal abstract class CircuitFactory
    {
        public abstract CircuitHost CreateCircuitHost(
            HttpContext httpContext,
            CircuitClientProxy client,
            string uriAbsolute,
            string baseUriAbsolute);
    }
}
