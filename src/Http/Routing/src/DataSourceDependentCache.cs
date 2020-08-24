// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    // FYI: This class is also linked into MVC. If you make changes to the API you will
    // also need to change MVC's usage.
    internal sealed class DataSourceDependentCache<T> : IDisposable where T : class
    {
        private readonly EndpointDataSource _dataSource;
        private readonly Func<IReadOnlyList<Endpoint>, T> _initializeCore;
        private readonly Func<T> _initializer;
        private readonly Action<object> _initializerWithState;

        private object _lock;
        private bool _initialized;
        private T _value;

        private IDisposable _disposable;
        private bool _disposed;

        public DataSourceDependentCache(EndpointDataSource dataSource, Func<IReadOnlyList<Endpoint>, T> initialize)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            if (initialize == null)
            {
                throw new ArgumentNullException(nameof(initialize));
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

                // Don't resubscribe if we're already disposed.
                if (_disposed)
                {
                    return _value;
                }

                _disposable = changeToken.RegisterChangeCallback(_initializerWithState, null);
                return _value;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposable?.Dispose();
                    _disposable = null;

                    // Tracking whether we're disposed or not prevents a race-condition
                    // between disposal and Initialize(). If a change callback fires after
                    // we dispose, then we don't want to reregister.
                    _disposed = true;
                }
            }
        }
    }
}
