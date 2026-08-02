// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding base64 encoded byte arrays.
/// </summary>
public class ByteArrayModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(byte[]))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new ByteArrayModelBinder(loggerFactory);
        }

        return null;
    }
}
