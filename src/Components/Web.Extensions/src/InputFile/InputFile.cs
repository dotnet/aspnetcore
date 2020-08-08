// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that wraps the HTML file input element and exposes a <see cref="Stream"/> for each file's contents.
    /// </summary>
    public class InputFile : ComponentBase, IInputFileJsCallbacks, IDisposable
    {
        private ElementReference _inputFileElement;

        private IJSRuntime _jsRuntime = default!;

        private IJSUnmarshalledRuntime? _jsUnmarshalledRuntime;

        private InputFileJsCallbacksRelay? _jsCallbacksRelay;

        [Inject]
        private IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        /// Gets or sets the event callback that will be invoked when the collection of selected files changes.
        /// </summary>
        [Parameter]
        public EventCallback<IFileListEntry[]> OnChange { get; set; }

        /// <summary>
        /// Gets or sets the maximum message size for file data sent over a circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        [Parameter]
        public int MaxMessageSize { get; set; } = 20 * 1024; // SignalR limit is 32K.

        /// <summary>
        /// Gets or sets the maximum internal buffer size for file data sent over a circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        [Parameter]
        public int MaxBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? AdditionalAttributes { get; set; }

        protected override void OnInitialized()
        {
            _jsRuntime = ServiceProvider.GetRequiredService<IJSRuntime>();
            _jsUnmarshalledRuntime = ServiceProvider.GetService<IJSUnmarshalledRuntime>();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsCallbacksRelay = new InputFileJsCallbacksRelay(this);
                await _jsRuntime.InvokeVoidAsync(InputFileInterop.Init, _jsCallbacksRelay.DotNetReference, _inputFileElement);
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

        internal Stream OpenFileStream(FileListEntry file)
            => _jsUnmarshalledRuntime != null ?
                (Stream)new SharedMemoryFileListEntryStream(_jsRuntime, _jsUnmarshalledRuntime, _inputFileElement, file) :
                new RemoteFileListEntryStream(_jsRuntime, _inputFileElement, MaxMessageSize, MaxBufferSize, file);

        internal async Task<IFileListEntry> ConvertToImageFileAsync(FileListEntry file, string format, int maxWidth, int maxHeight)
        {
            var imageFile = await _jsRuntime.InvokeAsync<FileListEntry>(InputFileInterop.ToImageFile, _inputFileElement, file.Id, format, maxWidth, maxHeight);

            imageFile.Owner = this;

            return imageFile;
        }

        Task IInputFileJsCallbacks.NotifyChange(FileListEntry[] files)
        {
            foreach (var file in files)
            {
                file.Owner = this;
            }

            return OnChange.InvokeAsync(files);
        }

        void IDisposable.Dispose()
        {
            _jsCallbacksRelay?.Dispose();
        }
    }
}
