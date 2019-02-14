// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// bind-Value="@model.PropertyName"
        /// </example>
        [Parameter] T Value { get; set; }

        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter] Action<T> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets an expression that identifies the bound value.
        /// </summary>
        [Parameter] Expression<Func<T>> ValueExpression { get; set; }

        /// <summary>
        /// Gets the associated <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>.
        /// </summary>
        protected EditContext EditContext { get; private set; }

        /// <summary>
        /// Gets the <see cref="FieldIdentifier"/> for the bound value.
        /// </summary>
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

        /// <summary>
        /// Gets a string that indicates the status of the field being edited. This will include
        /// some combination of "modified", "valid", or "invalid", depending on the status of the field.
        /// </summary>
        protected string CssClass
        {
            get
            {
                var isValid = !EditContext.GetValidationMessages(FieldIdentifier).Any();
                if (EditContext.IsModified(FieldIdentifier))
                {
                    return isValid ? "modified valid" : "modified invalid";
                }
                else
                {
                    return isValid ? "valid" : "invalid";
                }
            }
        }

        /// <inheritdoc />
        public override Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);

            if (EditContext == null)
            {
                // This is the first run
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
                // Not the first run

                // We don't support changing EditContext because it's messy to be clearing up state and event
                // handlers for the previous one, and there's no strong use case. If a strong use case
                // emerges, we can consider changing this.
                throw new InvalidOperationException($"{GetType()} does not support changing the " +
                    $"{nameof(Forms.EditContext)} dynamically.");
            }

            // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
            return base.SetParametersAsync(ParameterCollection.Empty);
        }
    }
}
