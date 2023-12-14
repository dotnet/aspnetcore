// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// A <see cref="IModelBinderProvider"/> for types deriving from <see cref="Enum"/>.
/// </summary>
public class EnumTypeModelBinderProvider : IModelBinderProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeModelBinderProvider"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    /// <remarks>The <paramref name="options"/> parameter is currently ignored.</remarks>
    public EnumTypeModelBinderProvider(MvcOptions options)
    {
    }

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.IsEnum)
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new EnumTypeModelBinder(
                suppressBindingUndefinedValueToEnumType: true,
                context.Metadata.UnderlyingOrModelType,
                loggerFactory);
        }

        return null;
    }
}
