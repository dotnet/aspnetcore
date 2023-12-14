// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for <see cref="KeyValuePair{TKey, TValue}"/>.
/// </summary>
public class KeyValuePairModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        if (modelType.IsGenericType &&
            modelType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            var typeArguments = modelType.GenericTypeArguments;

            var keyMetadata = context.MetadataProvider.GetMetadataForType(typeArguments[0]);
            var keyBinder = context.CreateBinder(keyMetadata);

            var valueMetadata = context.MetadataProvider.GetMetadataForType(typeArguments[1]);
            var valueBinder = context.CreateBinder(valueMetadata);

            var binderType = typeof(KeyValuePairModelBinder<,>).MakeGenericType(typeArguments);
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return (IModelBinder)Activator.CreateInstance(binderType, keyBinder, valueBinder, loggerFactory)!;
        }

        return null;
    }
}
