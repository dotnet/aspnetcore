// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Forms
{
    // Note: there's no reason why developers strictly need to use this. It's equally valid to
    // put a @foreach(var message in context.GetValidationMessages()) { ... } inside a form.
    // This component is for convenience only, plus it implements a few small perf optimizations.

    /// <summary>
    /// Displays a list of validation messages from a cascaded <see cref="EditContext"/>.
    /// </summary>
    public class ValidationSummary : ComponentBase, IDisposable
    {
        private EditContext _previousEditContext;
        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

        [CascadingParameter] EditContext CurrentEditContext { get; set; }

        /// <summary>`
        /// Constructs an instance of <see cref="ValidationSummary"/>.
        /// </summary>
        public ValidationSummary()
        {
            _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(ValidationSummary)} requires a cascading parameter " +
                    $"of type {nameof(EditContext)}. For example, you can use {nameof(ValidationSummary)} inside " +
                    $"an {nameof(EditForm)}.");
            }

            if (CurrentEditContext != _previousEditContext)
            {
                DetachValidationStateChangedListener();
                CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
                _previousEditContext = CurrentEditContext;
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // As an optimization, only evaluate the messages enumerable once, and
            // only produce the enclosing <ul> if there's at least one message
            var messagesEnumerator = CurrentEditContext.GetValidationMessages().GetEnumerator();
            if (messagesEnumerator.MoveNext())
            {
                builder.OpenElement(0, "ul");
                builder.AddAttribute(1, "class", "validation-errors");

                do
                {
                    builder.OpenElement(2, "li");
                    builder.AddAttribute(3, "class", "validation-message");
                    builder.AddContent(4, messagesEnumerator.Current);
                    builder.CloseElement();
                }
                while (messagesEnumerator.MoveNext());

                builder.CloseElement();
            }
        }

        private void HandleValidationStateChanged(object sender, ValidationStateChangedEventArgs eventArgs)
        {
            StateHasChanged();
        }

        void IDisposable.Dispose()
        {
            DetachValidationStateChangedListener();
        }

        private void DetachValidationStateChangedListener()
        {
            if (_previousEditContext != null)
            {
                _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }
        }
    }
}
