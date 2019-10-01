// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Displays a list of validation messages for a specified field within a cascaded <see cref="EditContext"/>.
    /// </summary>
    public class ValidationMessage<TValue> : ComponentBase, IDisposable
    {
        private EditContext _previousEditContext;
        private Expression<Func<TValue>> _previousFieldAccessor;
        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
        private FieldIdentifier _fieldIdentifier;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the created <c>div</c> element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

        [CascadingParameter] EditContext CurrentEditContext { get; set; }

        /// <summary>
        /// Specifies the field for which validation messages should be displayed.
        /// <para>
        /// Only one of <see cref="For"/> or <see cref="Model"/> should be specified.
        /// </para>
        /// </summary>
        [Parameter] public Expression<Func<TValue>> For { get; set; }

        /// <summary>
        /// Specifies the model for which validation messages should be displayed.
        /// <para>
        /// Only one of <see cref="For"/> or <see cref="Model"/> should be specified.
        /// </para>
        /// </summary>
        [Parameter] public TValue Model { get; set; }

        /// <summary>`
        /// Constructs an instance of <see cref="ValidationMessage{TValue}"/>.
        /// </summary>
        public ValidationMessage()
        {
            _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                    $"of type {nameof(EditContext)}. For example, you can use {GetType()} inside " +
                    $"an {nameof(EditForm)}.");
            }

            if (For == null && Model == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the " +
                    $"{nameof(For)} or {nameof(Model)} parameter.");
            }
            else if (For != null && Model != null)
            {
                throw new InvalidOperationException($"{GetType()} requires a {nameof(For)} " +
                   $"parameter, or an {nameof(Model)} parameter, but not both.");
            }

            if (For != null)
            {
                if (For != _previousFieldAccessor)
                {
                    _fieldIdentifier = FieldIdentifier.Create(For);
                    _previousFieldAccessor = For;
                }
            }
            else
            {
                _previousFieldAccessor = null;
                _fieldIdentifier = new FieldIdentifier(Model, string.Empty);
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
            foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, AdditionalAttributes);
                builder.AddAttribute(2, "class", "validation-message");
                builder.AddContent(3, message);
                builder.CloseElement();
            }
        }

        private void HandleValidationStateChanged(object sender, ValidationStateChangedEventArgs eventArgs)
        {
            StateHasChanged();
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        void IDisposable.Dispose()
        {
            DetachValidationStateChangedListener();
            Dispose(disposing: true);
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
