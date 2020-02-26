// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory.Infrastructure
{
    internal class TestExpirationToken : IChangeToken
    {
        private bool _hasChanged;
        private bool _activeChangeCallbacks;

        public bool HasChanged
        {
            get
            {
                HasChangedWasCalled = true;
                return _hasChanged;
            }
            set
            {
                _hasChanged = value;
            }
        }

        public bool HasChangedWasCalled { get; set; }

        public bool ActiveChangeCallbacks
        {
            get
            {
                ActiveChangeCallbacksWasCalled = true;
                return _activeChangeCallbacks;
            }
            set
            {
                _activeChangeCallbacks = value;
            }
        }

        public bool ActiveChangeCallbacksWasCalled { get; set; }

        public TokenCallbackRegistration Registration { get; set; }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            Registration = new TokenCallbackRegistration()
            {
                RegisteredCallback = callback,
                RegisteredState = state,
            };
            return Registration;
        }

        public void Fire()
        {
            HasChanged = true;
            if (Registration != null && !Registration.Disposed)
            {
                Registration.RegisteredCallback(Registration.RegisteredState);
            }
        }
    }
}