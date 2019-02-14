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
        [CascadingParameter] EditContext CascadedEditContext { get; set; }

        [Parameter] T Value { get; set; }

        [Parameter] Action<T> ValueChanged { get; set; }

        [Parameter] Expression<Func<T>> ValueExpression { get; set; }

        protected EditContext EditContext { get; private set; }

        protected FieldIdentifier FieldIdentifier { get; private set; }

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
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (EditContext == null)
            {
                // This is the first render
                // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInit to call base.OnInit()

                if (CascadedEditContext == null)
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

                EditContext = CascadedEditContext;
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
            }
            else if (CascadedEditContext != EditContext)
            {
                // Not the first render

                // We don't support changing EditContext because it's messy to be clearing up state and event
                // handlers for the previous one, and there's no strong use case. If a strong use case
                // emerges, we can consider changing this.
                throw new InvalidOperationException($"{GetType()} does not support changing the " +
                    $"{nameof(Forms.EditContext)} dynamically.");
            }
        }
    }
}
