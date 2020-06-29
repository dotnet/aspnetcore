// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component used for selecting a value from a group of choices.
    /// </summary>
    public class InputRadio<TValue> : InputChoice<TValue>
    {
        /// <summary>
        /// Gets the name of this <see cref="InputRadio{TValue}"/> group.
        /// </summary>
        protected string? Name { get; private set; }

        /// <summary>
        /// Gets or sets the value that will be bound when this radio input is selected.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        [Parameter]
        public TValue SelectedValue { get; set; } = default;

        /// <summary>
        /// Gets or sets group name inherited from an ancestor <see cref="InputRadioGroup"/>.
        /// </summary>
        [CascadingParameter(Name = nameof(InputRadioGroup.Name))]
        public string? CascadedName { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            Name = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("name", out var nameAttribute) ?
                Convert.ToString(nameAttribute) :
                CascadedName;

            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new InvalidOperationException($"{GetType()} requires either an explicit 'name' attribute or " +
                    $"a cascading parameter 'Name'. Normally this is provided by an ancestor {nameof(InputRadioGroup)}.");
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Debug.Assert(Name != null);

            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", "radio");
            builder.AddAttribute(3, "class", CssClass);
            builder.AddAttribute(4, "name", Name);
            builder.AddAttribute(5, "value", BindConverter.FormatValue(FormatValueAsString(SelectedValue)));
            builder.AddAttribute(6, "checked", SelectedValue?.Equals(CurrentValue));
            builder.AddAttribute(7, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.CloseElement();
        }
    }
}
