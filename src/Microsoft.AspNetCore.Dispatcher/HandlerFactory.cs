// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A simple implementation of <see cref="IHandlerFactory"/> that adapts a particular endpoint
    /// type to a handler.
    /// </summary>
    public sealed class HandlerFactory<TEndpoint> : IHandlerFactory
    {
        private readonly Func<TEndpoint, Func<RequestDelegate, RequestDelegate>> _adapter;

        public HandlerFactory(Func<TEndpoint, Func<RequestDelegate, RequestDelegate>> adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            _adapter = adapter;
        }

        public Func<RequestDelegate, RequestDelegate> CreateHandler(Endpoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (endpoint is TEndpoint myTypeOfEndpoint)
            {
                return _adapter(myTypeOfEndpoint);
            }

            return null;
        }
    }
}
