// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding <see cref="IDictionary{TKey, TValue}"/>.
/// </summary>
public class DictionaryModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        var dictionaryType = ClosedGenericMatcher.ExtractGenericInterface(modelType, typeof(IDictionary<,>));
        if (dictionaryType != null)
        {
            var binderType = typeof(DictionaryModelBinder<,>).MakeGenericType(dictionaryType.GenericTypeArguments);

            var keyType = dictionaryType.GenericTypeArguments[0];
            var keyBinder = context.CreateBinder(context.MetadataProvider.GetMetadataForType(keyType));

            var valueType = dictionaryType.GenericTypeArguments[1];
            var valueBinder = context.CreateBinder(context.MetadataProvider.GetMetadataForType(valueType));

            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var mvcOptions = context.Services.GetRequiredService<IOptions<MvcOptions>>().Value;
            return (IModelBinder)Activator.CreateInstance(
                binderType,
                keyBinder,
                valueBinder,
                loggerFactory,
                true /* allowValidatingTopLevelNodes */,
                mvcOptions)!;
        }

        return null;
    }
}
