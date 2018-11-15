// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class DisposableAction : IDisposable
    {
        public static readonly DisposableAction Empty = new DisposableAction(() => { });

        private Action<object> _action;
        private readonly object _state;

        public DisposableAction(Action action)
            : this(state => ((Action)state).Invoke(), state: action)
        {
        }

        public DisposableAction(Action<object> action, object state)
        {
            _action = action;
            _state = state;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Exchange(ref _action, (state) => { }).Invoke(_state);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
