// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

internal sealed class VirtualizeJsInterop : IAsyncDisposable
{
    private const string JsFunctionsPrefix = "Blazor._internal.Virtualize";

    private readonly IVirtualizeJsCallbacks _owner;

    private readonly IJSRuntime _jsRuntime;

    private DotNetObjectReference<VirtualizeJsInterop>? _selfReference;

    [DynamicDependency(nameof(OnSpacerBeforeVisible))]
    [DynamicDependency(nameof(OnSpacerAfterVisible))]
    public VirtualizeJsInterop(IVirtualizeJsCallbacks owner, IJSRuntime jsRuntime)
    {
        _owner = owner;
        _jsRuntime = jsRuntime;
    }

    public async ValueTask InitializeAsync(ElementReference spacerBefore, ElementReference spacerAfter)
    {
        _selfReference = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.init", _selfReference, spacerBefore, spacerAfter);
    }

    [JSInvokable]
    public void OnSpacerBeforeVisible(float spacerSize, float spacerSeparation, float containerSize)
    {
        _owner.OnBeforeSpacerVisible(spacerSize, spacerSeparation, containerSize);
    }

    [JSInvokable]
    public void OnSpacerAfterVisible(float spacerSize, float spacerSeparation, float containerSize)
    {
        _owner.OnAfterSpacerVisible(spacerSize, spacerSeparation, containerSize);
    }

    public async ValueTask DisposeAsync()
    {
        if (_selfReference != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.dispose", _selfReference);
            }
            catch (JSDisconnectedException)
            {
                // If the browser is gone, we don't need it to clean up any browser-side state
            }
        }
    }
}
