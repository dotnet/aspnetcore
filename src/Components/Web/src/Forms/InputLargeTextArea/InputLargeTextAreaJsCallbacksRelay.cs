// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class InputLargeTextAreaJsCallbacksRelay : IDisposable
    {
        private readonly IInputLargeTextAreaJsCallbacks _callbacks;

        public IDisposable DotNetReference { get; }

        [DynamicDependency(nameof(NotifyChange))]
        public InputLargeTextAreaJsCallbacksRelay(IInputLargeTextAreaJsCallbacks callbacks)
        {
            _callbacks = callbacks;

            DotNetReference = DotNetObjectReference.Create(this);
        }

        [JSInvokable]
        public Task NotifyChange(int length)
            => _callbacks.NotifyChange(length);

        public void Dispose()
        {
            DotNetReference.Dispose();
        }
    }
}
