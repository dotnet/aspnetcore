// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;

namespace DispatcherSample
{
    public class DispatcherFeature : IDispatcherFeature
    {
        private Endpoint _endpoint;
        private RequestDelegate _next;

        public Endpoint Endpoint
        {
            get
            {
                return _endpoint;
            }

            set
            {
                _endpoint = value;
            }
        }

        public RequestDelegate RequestDelegate
        {
            get
            {
                return _next;
            }

            set
            {
                _next = value;
            }
        }
    }
}
