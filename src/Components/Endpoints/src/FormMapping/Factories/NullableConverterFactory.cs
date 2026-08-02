// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class NullableConverterFactory : IFormDataConverterFactory
{
    public static readonly NullableConverterFactory Instance = new();

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType != null && options.ResolveConverter(underlyingType) != null;
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        Debug.Assert(underlyingType != null);

        var underlyingConverter = options.ResolveConverter(underlyingType);
        Debug.Assert(underlyingConverter != null);

        var expectedConverterType = typeof(NullableConverter<>).MakeGenericType(underlyingType);
        Debug.Assert(expectedConverterType != null);

        return Activator.CreateInstance(expectedConverterType, underlyingConverter) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for type '{type}'.");
    }
}
