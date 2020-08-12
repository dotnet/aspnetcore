// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that wraps the HTML file input element and exposes a <see cref="Stream"/> for each file's contents.
    /// </summary>
    public class InputFile : ComponentBase, IInputFileJsCallbacks, IDisposable
    {
        private ElementReference _inputFileElement;

        private IJSUnmarshalledRuntime? _jsUnmarshalledRuntime;

        private InputFileJsCallbacksRelay? _jsCallbacksRelay;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets the event callback that will be invoked when the collection of selected files changes.
        /// </summary>
        [Parameter]
        public EventCallback<InputFileChangeEventArgs> OnChange { get; set; }

        /// <summary>
        /// Gets or sets the maximum chunk size for file data sent over a SignalR circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        [Parameter]
        [UnsupportedOSPlatform("browser")]
        public int MaxSignalRChunkSize { get; set; } = 20 * 1024; // SignalR limit is 32K.

        /// <summary>
        /// Gets or sets the maximum internal buffer size for unread data sent over a SignalR circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        [Parameter]
        [UnsupportedOSPlatform("browser")]
        public int MaxUnreadMemoryBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? AdditionalAttributes { get; set; }

        protected override void OnInitialized()
        {
            _jsUnmarshalledRuntime = JSRuntime as IJSUnmarshalledRuntime;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsCallbacksRelay = new InputFileJsCallbacksRelay(this);
                await JSRuntime.InvokeVoidAsync(InputFileInterop.Init, _jsCallbacksRelay.DotNetReference, _inputFileElement);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", "file");
            builder.AddElementReferenceCapture(3, elementReference => _inputFileElement = elementReference);
            builder.CloseElement();
        }

        internal Stream OpenReadStream(BrowserFile file)
            => _jsUnmarshalledRuntime != null ?
                (Stream)new SharedBrowserFileStream(JSRuntime, _jsUnmarshalledRuntime, _inputFileElement, file) :
                new RemoteBrowserFileStream(JSRuntime, _inputFileElement, MaxSignalRChunkSize, MaxUnreadMemoryBufferSize, file);

        internal async Task<IBrowserFile> ConvertToImageFileAsync(BrowserFile file, string format, int maxWidth, int maxHeight)
        {
            var imageFile = await JSRuntime.InvokeAsync<BrowserFile>(InputFileInterop.ToImageFile, _inputFileElement, file.Id, format, maxWidth, maxHeight);

            imageFile.Owner = this;

            return imageFile;
        }

        Task IInputFileJsCallbacks.NotifyChange(BrowserFile[] files)
        {
            foreach (var file in files)
            {
                file.Owner = this;
            }

            return OnChange.InvokeAsync(new InputFileChangeEventArgs(files));
        }

        void IDisposable.Dispose()
        {
            _jsCallbacksRelay?.Dispose();
        }
    }
}
