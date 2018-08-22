// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    internal class DataSourceDependentCache<T> where T : class
    {
        private readonly EndpointDataSource _dataSource;
        private readonly Func<IReadOnlyList<Endpoint>, T> _initializeCore;
        private readonly Func<T> _initializer;
        private readonly Action<object> _initializerWithState;

        private object _lock;
        private bool _initialized;
        private T _value;

        public DataSourceDependentCache(EndpointDataSource dataSource, Func<IReadOnlyList<Endpoint>, T> initialize)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            _dataSource = dataSource;
            _initializeCore = initialize;

            _initializer = Initialize;
            _initializerWithState = (state) => Initialize();
            _lock = new object();
        }

        // Note that we don't lock here, and think about that in the context of a 'push'. So when data gets 'pushed'
        // we start computing a new state, but we're still able to perform operations on the old state until we've
        // processed the update.
        public T Value => _value;

        public T EnsureInitialized()
        {
            return LazyInitializer.EnsureInitialized<T>(ref _value, ref _initialized, ref _lock, _initializer);
        }

        private T Initialize()
        {
            lock (_lock)
            {
                var changeToken = _dataSource.GetChangeToken();
                _value = _initializeCore(_dataSource.Endpoints);

                changeToken.RegisterChangeCallback(_initializerWithState, null);
                return _value;
            }
        }
    }
}
