// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding <see cref="TimeOnly"/> and nullable <see cref="TimeOnly"/> models.
/// </summary>
public class TimeOnlyModelBinderProvider : IModelBinderProvider
{
    internal const DateTimeStyles SupportedStyles = DateTimeStyles.AllowWhiteSpaces;

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var modelType = context.Metadata.UnderlyingOrModelType;
        if (modelType == typeof(TimeOnly))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new TimeOnlyModelBinder(SupportedStyles, loggerFactory);
        }

        return null;
    }
}
