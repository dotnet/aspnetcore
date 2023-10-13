// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class EnumConverter<TEnum> : FormDataConverter<TEnum>, ISingleValueConverter<TEnum> where TEnum : struct, Enum
{
    public bool CanConvertSingleValue() => true;

    public bool TryConvertValue(ref FormDataReader reader, string value, out TEnum result)
    {
        if (Enum.TryParse(value, ignoreCase: true, out result))
        {
            return true;
        }
        else
        {
            var segment = reader.GetLastPrefixSegment();
            reader.AddMappingError(FormattableStringFactory.Create(FormDataResources.EnumMappingError, value, segment), value);
            result = default;
            return false;
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out TEnum result, out bool found)
    {
        found = reader.TryGetValue(out var value);
        if (!found)
        {
            result = default;
            return true;
        }

        return TryConvertValue(ref reader, value!, out result);
    }
}
