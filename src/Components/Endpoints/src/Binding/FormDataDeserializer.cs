// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal static class FormDataDeserializer
{
    public static T? Deserialize<T>(
        FormDataReader reader,
        FormDataSerializerOptions options)
    {
        try
        {
            var converter = options.ResolveConverter<T>();
            if (converter.TryRead(ref reader, typeof(T), options, out var result, out _))
            {
                return result;
            }

            // We don't do error handling yet.

            return default;
        }
        finally
        {
        }
    }
}
