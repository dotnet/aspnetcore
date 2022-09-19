// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class InputFileJsCallbacksRelay : IDisposable
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
