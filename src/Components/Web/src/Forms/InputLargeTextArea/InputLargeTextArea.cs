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
    /// content access without binding and without validations.
    /// </summary>
    public class InputLargeTextArea : ComponentBase, IDisposable
    {
        private const string JsFunctionsPrefix = "Blazor._internal.InputLargeTextArea.";
        private ElementReference _inputLargeTextAreaElement;
        private IDisposable? _dotNetReference;

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
                _dotNetReference = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync(JsFunctionsPrefix + "init", _dotNetReference, _inputLargeTextAreaElement);
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

        /// <summary>
        /// Invoked from the client when the textarea's onchange event occurs.
        /// </summary>
        /// <param name="length">The updated length of the textarea.</param>
        [JSInvokable]
        public Task NotifyChange(int length)
            => OnChange.InvokeAsync(new InputLargeTextAreaChangeEventArgs(this, length));

        /// <summary>
        /// Retrieves the textarea value asyncronously.
        /// </summary>
        /// <param name="maxLength">The maximum length of content to fetch from the textarea.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.</param>
        /// <returns>A <see cref="System.IO.TextReader"/> which facilitates reading of the textarea value.</returns>
        public virtual async ValueTask<StreamReader> GetTextAsync(int maxLength = 32_000, CancellationToken cancellationToken = default)
        {
            try
            {
                var streamRef = await JSRuntime.InvokeAsync<IJSStreamReference>(JsFunctionsPrefix + "getText", cancellationToken, _inputLargeTextAreaElement);
                var stream = await streamRef.OpenReadStreamAsync(maxLength, cancellationToken);
                var streamReader = new StreamReader(stream);
                return streamReader;
            }
            catch (JSException jsException)
            {
                // Special casing support for empty textareas. Due to security considerations
                // 0 length streams/textareas aren't permitted from JS->.NET Streaming Interop.
                if (jsException.InnerException is ArgumentOutOfRangeException outOfRangeException &&
                    outOfRangeException.ActualValue is not null &&
                    outOfRangeException.ActualValue is int actualLength &&
                    actualLength == 0)
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
        /// <param name="leaveTextAreaEnabled"><see langword="false" /> to disable the textarea while setting new content from the stream, otherwise <see langword="true" /> to allow it to be editable. Defaults to <see langword="false" />.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.</param>
        public virtual async ValueTask SetTextAsync(StreamWriter streamWriter, bool leaveTextAreaEnabled = false, CancellationToken cancellationToken = default)
        {
            if (streamWriter.Encoding.CodePage != Encoding.UTF8.CodePage)
            {
                throw new FormatException($"The encoding '{streamWriter.Encoding}' is not supported. SetTextAsync only allows UTF-8 encoded data.");
            }

            // Ensure we're reading from the beginning of the stream,
            // the StreamWriter.BaseStream.Position will be at the end by default
            var stream = streamWriter.BaseStream;
            if (stream.Position != 0)
            {
                stream.Position = 0;
            }

            try
            {
                if (!leaveTextAreaEnabled)
                {
                    await JSRuntime.InvokeVoidAsync(JsFunctionsPrefix + "enableTextArea", cancellationToken, _inputLargeTextAreaElement, /* disabled: */ true);
                }

                using var streamRef = new DotNetStreamReference(stream);
                await JSRuntime.InvokeVoidAsync(JsFunctionsPrefix + "setText", cancellationToken, _inputLargeTextAreaElement, streamRef);
            }
            finally
            {
                if (!leaveTextAreaEnabled)
                {
                    await JSRuntime.InvokeVoidAsync(JsFunctionsPrefix + "enableTextArea", cancellationToken, _inputLargeTextAreaElement, /* disabled: */ false);
                }
            }
        }

        void IDisposable.Dispose()
        {
            _dotNetReference?.Dispose();
        }
    }
}
