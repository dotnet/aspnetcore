// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding <see cref="DateOnly" /> and nullable <see cref="DateOnly"/> models.
/// </summary>
public class DateOnlyModelBinderProvider : IModelBinderProvider
{
    internal const DateTimeStyles SupportedStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var modelType = context.Metadata.UnderlyingOrModelType;
        if (modelType == typeof(DateOnly))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new DateOnlyModelBinder(SupportedStyles, loggerFactory);
        }

        return null;
    }
}
