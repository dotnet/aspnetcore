// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component used for selecting a value from a group of choices.
    /// </summary>
    public class InputRadio<TValue> : ComponentBase
    {
        /// <summary>
        /// Gets context for this <see cref="InputRadio{TValue}"/>.
        /// </summary>
        internal InputRadioContext? Context { get; private set; }

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets or sets the value of this input.
        /// </summary>
        [Parameter]
        public TValue? Value { get; set; }

        /// <summary>
        /// Gets or sets the name of the parent input radio group.
        /// </summary>
        [Parameter] public string? Name { get; set; }

        [CascadingParameter] private InputRadioContext? CascadedContext { get; set; }

        private string GetCssClass(string fieldClass)
        {
            if (AdditionalAttributes != null &&
                AdditionalAttributes.TryGetValue("class", out var @class) &&
                !string.IsNullOrEmpty(Convert.ToString(@class, CultureInfo.InvariantCulture)))
            {
                return $"{@class} {fieldClass}";
            }

            return fieldClass;
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            Context = string.IsNullOrEmpty(Name) ? CascadedContext : CascadedContext?.FindContextInAncestors(Name);

            if (Context == null)
            {
                throw new InvalidOperationException($"{GetType()} must have an ancestor {typeof(InputRadioGroup<TValue>)} " +
                    $"with a matching 'Name' property, if specified.");
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Debug.Assert(Context != null);

            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", GetCssClass(Context.FieldClass));
            builder.AddAttribute(3, "type", "radio");
            builder.AddAttribute(4, "name", Context.GroupName);
            builder.AddAttribute(5, "value", BindConverter.FormatValue(Value?.ToString()));
            builder.AddAttribute(6, "checked", Context.CurrentValue?.Equals(Value));
            builder.AddAttribute(7, "onchange", Context.ChangeEventCallback);
            builder.CloseElement();
        }
    }
}
