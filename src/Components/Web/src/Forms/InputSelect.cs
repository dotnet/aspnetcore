// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// A dropdown selection component.
    /// </summary>
    public class InputSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
    {
        /// <summary>
        /// Gets or sets the child content to be rendering inside the select element.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the <c>select</c> <see cref="ElementReference"/>.
        /// <para>
        /// May be <see langword="null"/> if accessed before the component is rendered.
        /// </para>
        /// </summary>
        [DisallowNull] public ElementReference? Element { get; protected set; }

        /// <summary>
        /// Gets or sets the current value of the input as a string array.
        /// </summary>
        protected string?[]? CurrentValueAsStringArray
        {
            get
            {
                if (CurrentValue is not Array array)
                {
                    return null;
                }
                
                return array
                    .Cast<object?>()
                    .Select(item => item?.ToString())
                    .ToArray();
            }
            set
            {
                CurrentValue = BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out var result)
                    ? result
                    : default;
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "select");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", CssClass);

            if (AdditionalAttributes?.ContainsKey("multiple") ?? false)
            {
                builder.AddAttribute(3, "value", BindConverter.FormatValue(CurrentValue)?.ToString());
                builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder(this, __value => CurrentValueAsStringArray = __value, CurrentValueAsStringArray));
            }
            else
            {
                builder.AddAttribute(5, "value", CurrentValueAsString);
                builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            }

            builder.AddElementReferenceCapture(7, __selectReference => Element = __selectReference);
            builder.AddContent(8, ChildContent);
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
            => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);
    }
}
