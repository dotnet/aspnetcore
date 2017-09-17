// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherOptions
    {
        public IList<RequestDelegate> Dispatchers { get; } = new List<RequestDelegate>();

        public IList<EndpointHandlerFactory> HandlerFactories { get; } = new List<EndpointHandlerFactory>();
    }
}
