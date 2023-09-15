// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal static partial class FormDataMapper
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public static T? Map<T>(
        FormDataReader reader,
        FormDataMapperOptions options)
    {
        FormDataConverter<T>? converter;
        try
        {
            converter = options.ResolveConverter<T>();
            if (converter == null)
            {
                Log.CannotResolveConverter(options.Logger, typeof(T), null);
                return default;
            }
        }
        catch (Exception ex)
        {
            Log.CannotResolveConverter(options.Logger, typeof(T), ex);
            return default;
        }

        if (converter.TryRead(ref reader, typeof(T), options, out var result, out _))
        {
            return result;
        }

        // Always return the result, even if it has failures. This is because we do not want
        // to loose the data that we were able to deserialize.
        return result;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Cannot resolve converter for type '{Type}'.", EventName = "CannotResolveConverter")]
        public static partial void CannotResolveConverter(ILogger logger, Type type, Exception? ex);
    }
}
