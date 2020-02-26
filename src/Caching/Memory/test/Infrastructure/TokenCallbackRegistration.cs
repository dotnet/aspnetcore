// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Caching.Memory.Infrastructure
{
    public class TokenCallbackRegistration : IDisposable
    {
        public Action<object> RegisteredCallback { get; set; }

        public object RegisteredState { get; set; }

        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}