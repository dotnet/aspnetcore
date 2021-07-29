// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
            => OnChange.InvokeAsync(new InputLargeTextAreaChangeEventArgs(length));

        /// <summary>
        /// Retrieves the textarea value asyncronously.
        /// </summary>
        /// <returns>The string value of the textarea.</returns>
        public ValueTask<string> GetTextAsync()
            => JSRuntime.InvokeAsync<string>(InputLargeTextAreaInterop.GetText, _inputLargeTextAreaElement);

        /// <summary>
        /// Sets the textarea value asyncronously.
        /// </summary>
        /// <param name="newValue">The new content to set for the textarea.</param>
        public ValueTask SetTextAsync(string newValue)
            => JSRuntime.InvokeVoidAsync(InputLargeTextAreaInterop.SetText, _inputLargeTextAreaElement, newValue);

        void IDisposable.Dispose()
        {
            _jsCallbacksRelay?.Dispose();
        }
    }
}
