// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherFeature : IDispatcherFeature
    {
        public Endpoint Endpoint { get; set; }

        public RequestDelegate RequestDelegate { get; set; }

        public DispatcherValueCollection Values { get; set; }
    }
}
