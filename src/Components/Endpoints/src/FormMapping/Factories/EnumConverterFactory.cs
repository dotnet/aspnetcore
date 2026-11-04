// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class EnumConverterFactory : IFormDataConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options) => type.IsEnum;

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        if (!CanConvert(type, options))
        {
            throw new InvalidOperationException($"Cannot create converter for type '{type}'.");
        }

        return (FormDataConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(type))!;
    }
}
