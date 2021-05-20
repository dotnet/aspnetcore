// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal static class InputExtensions
    {
        public static bool TryParseSelectableValueFromString<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>(
            this InputBase<TValue> input, string? value,
            [MaybeNullWhen(false)] out TValue result,
            [NotNullWhen(false)] out string? validationErrorMessage)
        {
            try
            {
                if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var parsedValue))
                {
                    result = parsedValue;
                    validationErrorMessage = null;
                    return true;
                }
                else
                {
                    result = default;
                    validationErrorMessage = $"The {input.DisplayName ?? input.FieldIdentifier.FieldName} field is not valid.";
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"{input.GetType()} does not support the type '{typeof(TValue)}'.", ex);
            }
        }
    }
}
