// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class InputFileJsCallbacksRelay : IDisposable
    {
        private readonly IInputFileJsCallbacks _callbacks;

        public IDisposable DotNetReference { get; }

        [DynamicDependency(nameof(NotifyChange))]
        public InputFileJsCallbacksRelay(IInputFileJsCallbacks callbacks)
        {
            _callbacks = callbacks;

            DotNetReference = DotNetObjectReference.Create(this);
        }

        [JSInvokable]
        public Task NotifyChange(BrowserFile[] files)
            => _callbacks.NotifyChange(files);

        public void Dispose()
        {
            DotNetReference.Dispose();
        }
    }
}
