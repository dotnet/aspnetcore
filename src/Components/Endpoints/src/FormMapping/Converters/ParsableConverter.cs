// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ParsableConverter<T> : FormDataConverter<T>, ISingleValueConverter<T> where T : IParsable<T>
{
    public bool CanConvertSingleValue() => true;

    public bool TryConvertValue(ref FormDataReader reader, string value, out T result)
    {
        if (T.TryParse(value, reader.Culture, out result!))
        {
            return true;
        }
        else
        {
            var segment = reader.GetLastPrefixSegment();
            reader.AddMappingError(FormattableStringFactory.Create(FormDataResources.ParsableMappingError, value, segment), value);
            result = default!;
            return false;
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        found = reader.TryGetValue(out var value);
        if (!found)
        {
            result = default;
            return true;
        }
        else
        {
            return TryConvertValue(ref reader, value!, out result!);
        }
    }
}
