// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class CancellationTokenExtensions
    {
        public static IDisposable SafeRegister(this CancellationToken cancellationToken, Action<object> callback, object state)
        {
            var callbackWrapper = new CancellationCallbackWrapper(callback, state);
            var registration = cancellationToken.Register(s => InvokeCallback(s), callbackWrapper);
            var disposeCancellationState = new DisposeCancellationState(callbackWrapper, registration);

            return new DisposableAction(s => Dispose(s), disposeCancellationState);
        }

        private static void InvokeCallback(object state)
        {
            ((CancellationCallbackWrapper)state).TryInvoke();
        }

        private static void Dispose(object state)
        {
            ((DisposeCancellationState)state).TryDispose();
        }

        private class DisposeCancellationState
        {
            private readonly CancellationCallbackWrapper _callbackWrapper;
            private readonly CancellationTokenRegistration _registration;

            public DisposeCancellationState(CancellationCallbackWrapper callbackWrapper, CancellationTokenRegistration registration)
            {
                _callbackWrapper = callbackWrapper;
                _registration = registration;
            }

            public void TryDispose()
            {
                if (_callbackWrapper.TrySetInvoked())
                {
                    _registration.Dispose();
                }
            }
        }

        private class CancellationCallbackWrapper
        {
            private readonly Action<object> _callback;
            private readonly object _state;
            private int _callbackInvoked;

            public CancellationCallbackWrapper(Action<object> callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            public bool TrySetInvoked()
            {
                return Interlocked.Exchange(ref _callbackInvoked, 1) == 0;
            }

            public void TryInvoke()
            {
                if (TrySetInvoked())
                {
                    _callback(_state);
                }
            }
        }
    }
}
