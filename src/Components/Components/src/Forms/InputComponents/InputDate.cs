// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;
using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    // TODO: Consider support for Nullable<DateTime>, Nullable<DateTimeOffset>
    //       otherwise it may be impossible to have optional date inputs

    /// <summary>
    /// An input component for editing date values.
    /// Supported types are <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.
    /// </summary>
    public class InputDate<T> : InputBase<T>
    {
        const string dateFormat = "yyyy-MM-dd"; // Compatible with HTML date inputs

        [Parameter] string ParsingErrorMessage { get; set; } = "The {0} field must be a date.";

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "date");
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(3, "value", BindMethods.GetValue(CurrentValueAsString));
            builder.AddAttribute(4, "onchange", BindMethods.SetValueHandler(__value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override string FormatValueAsString(T value)
        {
            if (typeof(T) == typeof(DateTime))
            {
                return ((DateTime)(object)value).ToString(dateFormat);
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                return ((DateTimeOffset)(object)value).ToString(dateFormat);
            }
            else
            {
                throw new InvalidOperationException($"The type '{typeof(T)}' is not a supported date type.");
            }
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage)
        {
            bool success;

            if (typeof(T) == typeof(DateTime))
            {
                success = TryParseDateTime(value, out result);
            }
            else if (typeof (T) == typeof(DateTimeOffset))
            {
                success = TryParseDateTimeOffset(value, out result);
            }
            else
            {
                throw new InvalidOperationException($"The type '{typeof(T)}' is not a supported date type.");
            }

            if (success)
            {
                validationErrorMessage = null;
                return true;
            }
            else
            {
                validationErrorMessage = string.Format(ParsingErrorMessage, FieldIdentifier.FieldName);
                return false;
            }
        }

        static bool TryParseDateTime(string value, out T result)
        {
            var success = DateTime.TryParse(value, out var parsedValue);
            if (success)
            {
                result = (T)(object)parsedValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        static bool TryParseDateTimeOffset(string value, out T result)
        {
            var success = DateTimeOffset.TryParse(value, out var parsedValue);
            if (success)
            {
                result = (T)(object)parsedValue;
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
