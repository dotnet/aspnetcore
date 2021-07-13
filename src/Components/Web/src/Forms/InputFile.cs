// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// A component that wraps the HTML file input element and supplies a <see cref="Stream"/> for each file's contents.
    /// </summary>
    public class InputFile : ComponentBase, IInputFileJsCallbacks, IDisposable
    {
        private ElementReference _inputFileElement;

        private IJSUnmarshalledRuntime? _jsUnmarshalledRuntime;

        private InputFileJsCallbacksRelay? _jsCallbacksRelay;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private IOptions<RemoteBrowserFileStreamOptions> Options { get; set; } = default!;

        /// <summary>
        /// Gets or sets the event callback that will be invoked when the collection of selected files changes.
        /// </summary>
        [Parameter]
        public EventCallback<InputFileChangeEventArgs> OnChange { get; set; }

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets or sets the associated <see cref="ElementReference"/>.
        /// <para>
        /// May be <see langword="null"/> if accessed before the component is rendered.
        /// </para>
        /// </summary>
        [DisallowNull]
        public ElementReference? Element
        {
            get => _inputFileElement;
            protected set => _inputFileElement = value!.Value;
        }

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _jsUnmarshalledRuntime = JSRuntime as IJSUnmarshalledRuntime;
        }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsCallbacksRelay = new InputFileJsCallbacksRelay(this);
                await JSRuntime.InvokeVoidAsync(InputFileInterop.Init, _jsCallbacksRelay.DotNetReference, _inputFileElement);
            }
        }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", "file");
            builder.AddElementReferenceCapture(3, elementReference => _inputFileElement = elementReference);
            builder.CloseElement();
        }

        internal Stream OpenReadStream(BrowserFile file, long maxAllowedSize, CancellationToken cancellationToken)
            => new BrowserFileStream(JSRuntime, _inputFileElement, file, Options.Value, maxAllowedSize, cancellationToken);

        internal async ValueTask<IBrowserFile> ConvertToImageFileAsync(BrowserFile file, string format, int maxWidth, int maxHeight)
        {
            var imageFile = await JSRuntime.InvokeAsync<BrowserFile>(InputFileInterop.ToImageFile, _inputFileElement, file.Id, format, maxWidth, maxHeight);

            if (imageFile is null)
            {
                throw new InvalidOperationException("ToImageFile returned an unexpected null result.");
            }

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
