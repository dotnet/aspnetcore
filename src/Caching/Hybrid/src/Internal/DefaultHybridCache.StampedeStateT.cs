// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{

    internal sealed class StampedeState<T> : StampedeState
    {
        public StampedeState(in StampedeKey key) : base(key) { }

        private readonly TaskCompletionSource<T> result = new();

        public Task<T> Task => result.Task;

        internal void SetException(Exception ex) => result.TrySetException(ex);

        internal void SetResult(T value) => result.TrySetResult(value);

        protected override void SetCanceled() => result.TrySetCanceled(SharedToken);
    }
}
