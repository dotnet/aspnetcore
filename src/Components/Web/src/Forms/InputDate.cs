// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component for editing date values.
    /// Supported types are <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.
    /// </summary>
    public class InputDate<TValue> : InputBase<TValue>
    {
        private const string DateFormat = "yyyy-MM-dd";                     // Compatible with HTML 'date' inputs
        private const string DateTimeLocalFormat = "yyyy-MM-ddTHH:mm:ss";   // Compatible with HTML 'datetime-local' inputs
        private const string MonthFormat = "yyyy-MM";                       // Compatible with HTML 'month' inputs
        private const string TimeFormat = "HH:mm:ss";                       // Compatible with HTML 'time' inputs

        private string _typeAttributeValue = default!;
        private string _format = default!;

        /// <summary>
        /// Gets or sets the error message used when displaying an a parsing error.
        /// </summary>
        [Parameter] public string ParsingErrorMessage { get; set; } = "The {0} field must be a date.";

        /// <summary>
        /// Gets or sets the associated <see cref="ElementReference"/>.
        /// <para>
        /// May be <see langword="null"/> if accessed before the component is rendered.
        /// </para>
        /// </summary>
        [DisallowNull] public ElementReference? Element { get; protected set; }

        /// <inheritdoc />
        public override Task SetParametersAsync(ParameterView parameters)
        {
            _typeAttributeValue = parameters.TryGetValue<string>("type", out var typeAttributeValue)
                ? typeAttributeValue
                : "date";

            _format = _typeAttributeValue switch
            {
                "date" => DateFormat,
                "datetime-local" => DateTimeLocalFormat,
                "month" => MonthFormat,
                "time" => TimeFormat,
                _ => throw new InvalidOperationException($"Unsupported 'type' attribute value '{_typeAttributeValue}'.")
            };

            return base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", _typeAttributeValue);
            builder.AddAttribute(3, "class", CssClass);
            builder.AddAttribute(4, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(5, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.AddElementReferenceCapture(6, __inputReference => Element = __inputReference);
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override string FormatValueAsString(TValue? value)
            => value switch
            {
                DateTime dateTimeValue => BindConverter.FormatValue(dateTimeValue, _format, CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffsetValue => BindConverter.FormatValue(dateTimeOffsetValue, _format, CultureInfo.InvariantCulture),
                DateOnly dateOnlyValue => BindConverter.FormatValue(dateOnlyValue, _format, CultureInfo.InvariantCulture),
                TimeOnly timeOnlyValue => BindConverter.FormatValue(timeOnlyValue, _format, CultureInfo.InvariantCulture),
                _ => string.Empty, // Handles null for Nullable<DateTime>, etc.
            };

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            // Unwrap nullable types. We don't have to deal with receiving empty values for nullable
            // types here, because the underlying InputBase already covers that.
            var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

            bool success;
            if (targetType == typeof(DateTime))
            {
                success = TryParseDateTime(value, out result);
            }
            else if (targetType == typeof(DateTimeOffset))
            {
                success = TryParseDateTimeOffset(value, out result);
            }
            else if (targetType == typeof(DateOnly))
            {
                success = TryParseDateOnly(value, out result);
            }
            else if (targetType == typeof(TimeOnly))
            {
                success = TryParseTimeOnly(value, out result);
            }
            else
            {
                throw new InvalidOperationException($"The type '{targetType}' is not a supported date type.");
            }

            if (success)
            {
                Debug.Assert(result != null);
                validationErrorMessage = null;
                return true;
            }
            else
            {
                validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
                return false;
            }
        }

        private bool TryParseDateTime(string? value, [MaybeNullWhen(false)] out TValue result)
        {
            var success = BindConverter.TryConvertToDateTime(value, CultureInfo.InvariantCulture, _format, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private bool TryParseDateTimeOffset(string? value, [MaybeNullWhen(false)] out TValue result)
        {
            var success = BindConverter.TryConvertToDateTimeOffset(value, CultureInfo.InvariantCulture, _format, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private bool TryParseDateOnly(string? value, [MaybeNullWhen(false)] out TValue result)
        {
            var success = BindConverter.TryConvertToDateOnly(value, CultureInfo.InvariantCulture, _format, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private bool TryParseTimeOnly(string? value, [MaybeNullWhen(false)] out TValue result)
        {
            var success = BindConverter.TryConvertToTimeOnly(value, CultureInfo.InvariantCulture, _format, out var parsedValue);
            if (success)
            {
                result = (TValue)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
