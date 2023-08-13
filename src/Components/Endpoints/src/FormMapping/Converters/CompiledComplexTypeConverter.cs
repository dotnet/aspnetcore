// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class CompiledComplexTypeConverter<T>(CompiledComplexTypeConverter<T>.ConverterDelegate body) : FormDataConverter<T>
{
    public delegate bool ConverterDelegate(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found);

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        result = default;
        found = false;

        try
        {
            return body(ref context, type, options, out result, out found);
        }
        catch (Exception ex)
        {
            context.AddMappingError(ex, null);
            return false;
        }
    }
}
