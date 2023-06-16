// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class ParsableConverterFactory : IFormDataConverterFactory
{
    public static readonly ParsableConverterFactory Instance = new();

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        return ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IParsable<>)) is not null;
    }

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        return Activator.CreateInstance(typeof(ParsableConverter<>).MakeGenericType(type)) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for '{type.FullName}'.");
    }
}
