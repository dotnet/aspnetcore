// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http
{
    internal class DefaultTypedHttpClientFactory<TClient> : ITypedHttpClientFactory<TClient>
    {
        private readonly static Func<ObjectFactory> _createActivator = () => ActivatorUtilities.CreateFactory(typeof(TClient), new Type[] { typeof(HttpClient), });
        private readonly IServiceProvider _services;

        private ObjectFactory _activator;
        private bool _initialized;
        private object _lock;

        public DefaultTypedHttpClientFactory(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public TClient CreateClient(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            LazyInitializer.EnsureInitialized(ref _activator, ref _initialized, ref _lock, _createActivator);
            return (TClient)_activator(_services, new object[] { httpClient });
        }
    }
}
