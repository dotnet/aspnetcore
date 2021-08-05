// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// A multiline input component for editing large <see cref="string"/> values, supports async
    /// content access without binding nor validations.
    /// </summary>
    public class InputLargeTextArea : ComponentBase, IInputLargeTextAreaJsCallbacks, IDisposable
    {
        private ElementReference _inputLargeTextAreaElement;

        private InputLargeTextAreaJsCallbacksRelay? _jsCallbacksRelay;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets or sets the event callback that will be invoked when the textarea content changes.
        /// </summary>
        [Parameter]
        public EventCallback<InputLargeTextAreaChangeEventArgs> OnChange { get; set; }

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
            get => _inputLargeTextAreaElement;
            protected set => _inputLargeTextAreaElement = value!.Value;
        }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsCallbacksRelay = new InputLargeTextAreaJsCallbacksRelay(this);
                await JSRuntime.InvokeVoidAsync(InputLargeTextAreaInterop.Init, _jsCallbacksRelay.DotNetReference, _inputLargeTextAreaElement);
            }
        }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "textarea");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddElementReferenceCapture(2, elementReference => _inputLargeTextAreaElement = elementReference);
            builder.CloseElement();
        }

        Task IInputLargeTextAreaJsCallbacks.NotifyChange(int length)
            => OnChange.InvokeAsync(new InputLargeTextAreaChangeEventArgs(this, length));

        /// <summary>
        /// Retrieves the textarea value asyncronously.
        /// </summary>
        /// <param name="maxLength">The maximum length of content to fetch from the textarea.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.</param>
        /// <returns>A <see cref="System.IO.TextReader"/> which facilitates reading of the textarea value.</returns>
        public async ValueTask<StreamReader> GetTextAsync(int maxLength = 32_000, CancellationToken cancellationToken = default)
        {
            try
            {
                var streamRef = await JSRuntime.InvokeAsync<IJSStreamReference>(InputLargeTextAreaInterop.GetText, cancellationToken, _inputLargeTextAreaElement);
                var stream = await streamRef.OpenReadStreamAsync(maxLength, cancellationToken);
                var streamReader = new StreamReader(stream);
                return streamReader;
            }
            catch (JSException jsException)
            {
                // Special casing support for empty textareas. Due to security considerations
                // 0 length streams/textareas aren't permitted from JS->.NET Streaming Interop.
                if (jsException.InnerException is ArgumentOutOfRangeException)
                {
                    return StreamReader.Null;
                }

                throw;
            }
        }

        /// <summary>
        /// Sets the textarea value asyncronously.
        /// </summary>
        /// <param name="streamWriter">A <see cref="System.IO.StreamWriter"/> used to set the value of the textarea.</param>
        /// <param name="leaveTextAreaEnabled">Don't disable the textarea while settings the new textarea value from the stream.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.</param>
        public async ValueTask SetTextAsync(StreamWriter streamWriter, bool leaveTextAreaEnabled = false, CancellationToken cancellationToken = default)
        {
            if (streamWriter.Encoding is not UTF8Encoding)
            {
                throw new FormatException($"Expected {typeof(UTF8Encoding)}, got ${streamWriter.Encoding}");
            }

            try
            {
                if (!leaveTextAreaEnabled)
                {
                    await JSRuntime.InvokeVoidAsync(InputLargeTextAreaInterop.EnableTextArea, cancellationToken, _inputLargeTextAreaElement, /* disabled: */ true);
                }

                // Ensure we're reading from the beginning of the stream,
                // the StreamWriter.BaseStream.Position will be at the end by default
                var stream = streamWriter.BaseStream;
                if (stream.Position != 0)
                {
                    if (!stream.CanSeek)
                    {
                        throw new NotSupportedException("Unable to read from the beginning of the stream.");
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                }

                using var streamRef = new DotNetStreamReference(stream);
                await JSRuntime.InvokeVoidAsync(InputLargeTextAreaInterop.SetText, cancellationToken, _inputLargeTextAreaElement, streamRef);
            }
            finally
            {
                if (!leaveTextAreaEnabled)
                {
                    await JSRuntime.InvokeVoidAsync(InputLargeTextAreaInterop.EnableTextArea, cancellationToken, _inputLargeTextAreaElement, /* disabled: */ false);
                }
            }
        }

        void IDisposable.Dispose()
        {
            _jsCallbacksRelay?.Dispose();
        }
    }
}
