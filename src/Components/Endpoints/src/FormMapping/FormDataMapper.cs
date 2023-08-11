// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal static class FormDataMapper
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public static T? Map<T>(
        FormDataReader reader,
        FormDataMapperOptions options)
    {
        try
        {
            var converter = options.ResolveConverter<T>();
            if (converter.TryRead(ref reader, typeof(T), options, out var result, out _))
            {
                return result;
            }

            // Always return the result, even if it has failures. This is because we do not want
            // to loose the data that we were able to deserialize.
            return result;
        }
        finally
        {
        }
    }
}
