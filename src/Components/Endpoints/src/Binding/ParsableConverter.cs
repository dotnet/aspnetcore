// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class ParsableConverterFactory : IFormDataConverterFactory
{
    public static bool CanConvert(Type type, FormDataSerializerOptions options)
    {
        // returns whether type implements IParsable<T>
        return typeof(IParsable<>).MakeGenericType(type).IsAssignableFrom(type);
    }

    public static FormDataConverter CreateConverter(Type type, FormDataSerializerOptions options)
    {
        return typeof(ParsableConverter<>)
            .MakeGenericType(type)
            .GetConstructor(Type.EmptyTypes)!
            .Invoke(null) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}

internal class ParsableConverter<T> : FormDataConverter<T>, ISingleValueConverter where T : IParsable<T>
{
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataSerializerOptions options, out T? result, out bool found)
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
