// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class ParsableConverter<T> : FormDataConverter<T>, ISingleValueConverter where T : IParsable<T>
{
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        found = reader.TryGetValue(out var value);
        if (found && T.TryParse(value, reader.Culture, out result))
        {
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }
}
