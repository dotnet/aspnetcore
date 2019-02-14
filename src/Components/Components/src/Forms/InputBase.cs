// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// A base class for form input components. This base class automatically
    /// integrates with an <see cref="Forms.EditContext"/>, which must be supplied
    /// as a cascading parameter.
    /// </summary>
    public abstract class InputBase<T> : ComponentBase
    {
        private EditContext _fixedEditContext;
        private FieldIdentifier _fieldIdentifier;

        [CascadingParameter] EditContext EditContext { get; set; }

        [Parameter] T Value { get; set; }

        [Parameter] Action<T> ValueChanged { get; set; }

        [Parameter] Expression<Func<T>> ValueExpression { get; set; }

        /// <summary>
        /// Gets or sets the current value of the input.
        /// </summary>
        protected T CurrentValue
        {
            get => Value;
            set
            {
                var hasChanged = !EqualityComparer<T>.Default.Equals(value, Value);
                if (hasChanged)
                {
                    Value = value;
                    ValueChanged?.Invoke(value);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnInit()
        {
            if (EditContext == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                    $"of type {nameof(Forms.EditContext)}. For example, you can use {GetType().FullName} inside " +
                    $"an {nameof(EditForm)}.");
            }

            if (ValueExpression == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                    $"parameter. Normally this is provided automatically when using 'bind-Value'.");
            }

            _fixedEditContext = EditContext;
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (EditContext != _fixedEditContext)
            {
                // We're not supporting it just because it's messy to be clearing up state and event
                // handlers for the previous one, and there's no strong use case. If a strong use case
                // emerges, we can consider changing this.
                throw new InvalidOperationException($"{GetType()} does not support changing the " +
                    $"{nameof(Forms.EditContext)} dynamically.");
            }
        }
    }
}
