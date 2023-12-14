// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding <see cref="decimal"/>, <see cref="double"/>,
/// <see cref="float"/>, and their <see cref="Nullable{T}"/> wrappers.
/// </summary>
public class FloatingPointTypeModelBinderProvider : IModelBinderProvider
{
    // SimpleTypeModelBinder uses DecimalConverter and similar. Those TypeConverters default to NumberStyles.Float.
    // Internal for testing.
    internal const NumberStyles SupportedStyles = NumberStyles.Float | NumberStyles.AllowThousands;

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.UnderlyingOrModelType;
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        if (modelType == typeof(decimal))
        {
            return new DecimalModelBinder(SupportedStyles, loggerFactory);
        }

        if (modelType == typeof(double))
        {
            return new DoubleModelBinder(SupportedStyles, loggerFactory);
        }

        if (modelType == typeof(float))
        {
            return new FloatModelBinder(SupportedStyles, loggerFactory);
        }

        return null;
    }
}
