// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class ParsableConverterFactory : IFormDataConverterFactory
{
    public static readonly ParsableConverterFactory Instance = new();

    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        return ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IParsable<>)) is not null;
    }

    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        return Activator.CreateInstance(typeof(ParsableConverter<>).MakeGenericType(type)) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}
