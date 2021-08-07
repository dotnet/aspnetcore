// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Infrastructure
{
    public class JSEventCallbackRelay : IHandleEvent
    {
        private readonly IJSObjectReference _jsObjectReference;

        public EventCallback Callback { get; }
    
        public JSEventCallbackRelay(IJSObjectReference jsObjectReference)
        {
            _jsObjectReference = jsObjectReference;

            Callback = new EventCallback(this, null);
        }

        Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem item, object? arg)
        {
            return _jsObjectReference.InvokeVoidAsync("callback").AsTask();
        }
    }
}
