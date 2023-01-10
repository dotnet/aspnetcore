// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding <see cref="DateTime" /> and nullable <see cref="DateTime"/> models.
/// </summary>
public class DateTimeModelBinderProvider : IModelBinderProvider
{
    internal const DateTimeStyles SupportedStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.UnderlyingOrModelType;
        if (modelType == typeof(DateTime))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new DateTimeModelBinder(SupportedStyles, loggerFactory);
        }

        return null;
    }
}
