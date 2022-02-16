// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding types that have a TryParse method.
/// </summary>
public sealed class TryParseModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.IsParseableType)
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();

            var binderType = typeof(TryParseModelBinder<>).MakeGenericType(context.Metadata.ModelType);
            return (IModelBinder)Activator.CreateInstance(binderType, loggerFactory)!;
        }

        return null;
    }
}
