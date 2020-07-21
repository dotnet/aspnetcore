// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for detecting and relaying virtualization events on the web.
    /// </summary>
    public class WebVirtualizationHelper : IVirtualizationHelper
    {
        private readonly IJSRuntime _jsRuntime;

        private readonly DotNetObjectReference<WebVirtualizationHelper> _selfReference;

        /// <inheritdoc />
        public event EventHandler<SpacerEventArgs>? TopSpacerVisible;

        /// <inheritdoc />
        public event EventHandler<SpacerEventArgs>? BottomSpacerVisible;

        /// <summary>
        /// Instantiates a new <see cref="WebVirtualizationHelper"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> dependency.</param>
        public WebVirtualizationHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _selfReference = DotNetObjectReference.Create(this);
        }

        /// <summary>
        /// Called when the top spacer becomes visible, invoking <see cref="TopSpacerVisible"/>.
        /// This method is intended to be invoked only from JavaScript.
        /// </summary>
        /// <param name="spacerSize">The new top spacer size.</param>
        /// <param name="containerSize">The top spacer's container size.</param>
        [JSInvokable]
        public void OnTopSpacerVisible(float spacerSize, float containerSize)
        {
            TopSpacerVisible?.Invoke(this, new SpacerEventArgs(spacerSize, containerSize));
        }

        /// <summary>
        /// Called when the bottom spacer becomes visible, invoking <see cref="BottomSpacerVisible"/>.
        /// This method is intended to be invoked only from JavaScript.
        /// </summary>
        /// <param name="spacerSize">The new bottom spacer size.</param>
        /// <param name="containerSize">The bottom spacer's container size.</param>
        [JSInvokable]
        public void OnBottomSpacerVisible(float spacerSize, float containerSize)
        {
            BottomSpacerVisible?.Invoke(this, new SpacerEventArgs(spacerSize, containerSize));
        }

        /// <inheritdoc />
        public async ValueTask InitAsync(ElementReference topSpacer, ElementReference bottomSpacer)
        {
            await _jsRuntime.InvokeVoidAsync("Blazor._internal.Virtualize.init", _selfReference, topSpacer, bottomSpacer);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _jsRuntime.InvokeVoidAsync("Blazor._internal.Virtualize.dispose", _selfReference);
        }
    }
}
