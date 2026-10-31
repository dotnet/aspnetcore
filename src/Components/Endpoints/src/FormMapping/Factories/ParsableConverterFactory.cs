// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ParsableConverterFactory : IFormDataConverterFactory
{
    public static readonly ParsableConverterFactory Instance = new();

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        return ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IParsable<>)) is not null;
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        return Activator.CreateInstance(typeof(ParsableConverter<>).MakeGenericType(type)) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}
