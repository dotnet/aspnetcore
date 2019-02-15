// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;
using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component for editing numeric values.
    /// Supported numeric types are <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>.
    /// </summary>
    public class InputNumber<T> : InputBase<T>
    {
        delegate bool Parser(string value, out T result);
        private static Parser _parser;

        // Determine the parsing logic once per T and cache it, so we don't have to consider all the possible types on each parse
        static InputNumber()
        {
            // Unwrap Nullable<T>, because InputBase already deals with the Nullable aspect
            // of it for us. We will only get asked to parse the T for nonempty inputs.
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType == typeof(short))
            {
                _parser = TryParseShort;
            }
            else if (targetType == typeof(int))
            {
                _parser = TryParseInt;
            }
            else if (targetType == typeof(long))
            {
                _parser = TryParseLong;
            }
            else if (targetType == typeof(float))
            {
                _parser = TryParseFloat;
            }
            else if (targetType == typeof(double))
            {
                _parser = TryParseDouble;
            }
            else if (targetType == typeof(decimal))
            {
                _parser = TryParseDecimal;
            }
            else
            {
                throw new InvalidOperationException($"The type '{targetType}' is not a supported numeric type.");
            }
        }

        [Parameter] string ParsingErrorMessage { get; set; } = "The {0} field must be a number.";

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "class", CssClass);
            builder.AddAttribute(2, "value", BindMethods.GetValue(CurrentValueAsString));
            builder.AddAttribute(3, "onchange", BindMethods.SetValueHandler(__value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage)
        {
            if (_parser(value, out result))
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

        static bool TryParseShort(string value, out T result)
        {
            var success = short.TryParse(value, out var parsedValue);
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

        static bool TryParseInt(string value, out T result)
        {
            var success = int.TryParse(value, out var parsedValue);
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

        static bool TryParseLong(string value, out T result)
        {
            var success = long.TryParse(value, out var parsedValue);
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

        static bool TryParseFloat(string value, out T result)
        {
            var success = float.TryParse(value, out var parsedValue);
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

        static bool TryParseDouble(string value, out T result)
        {
            var success = double.TryParse(value, out var parsedValue);
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

        static bool TryParseDecimal(string value, out T result)
        {
            var success = decimal.TryParse(value, out var parsedValue);
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
