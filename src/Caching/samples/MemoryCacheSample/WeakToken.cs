// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    public class WeakToken<T> : IChangeToken where T : class
    {
        private WeakReference<T> _reference;

        public WeakToken(WeakReference<T> reference)
        {
            _reference = reference;
        }

        public bool ActiveChangeCallbacks
        {
            get { return false; }
        }

        public bool HasChanged
        {
            get
            {
                return !_reference.TryGetTarget(out T ignored);
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            throw new NotSupportedException();
        }
    }
}
