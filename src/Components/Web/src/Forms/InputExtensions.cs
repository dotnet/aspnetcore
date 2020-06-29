// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms
{
    static class InputExtensions
    {
        public static bool TryParseSelectableValueFromString<TValue>(this InputBase<TValue> input, string? value, [MaybeNull] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            if (typeof(TValue) == typeof(string))
            {
                result = (TValue)(object?)value;
                validationErrorMessage = null;
                return true;
            }
            else if (typeof(TValue).IsEnum || (Nullable.GetUnderlyingType(typeof(TValue))?.IsEnum ?? false))
            {
                var success = BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var parsedValue);
                if (success)
                {
                    result = parsedValue;
                    validationErrorMessage = null;
                    return true;
                }
                else
                {
                    result = default;
                    validationErrorMessage = $"The {input.FieldIdentifier.FieldName} field is not valid.";
                    return false;
                }
            }

            throw new InvalidOperationException($"{input.GetType()} does not support the type '{typeof(TValue)}'.");
        }
    }
}
