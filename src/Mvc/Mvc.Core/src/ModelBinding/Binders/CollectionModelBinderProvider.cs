// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for <see cref="ICollection{T}"/>.
/// </summary>
public class CollectionModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;

        // Arrays are handled by another binder.
        if (modelType.IsArray)
        {
            return null;
        }

        // If the model type is ICollection<> then we can call its Add method, so we can always support it.
        var collectionType = ClosedGenericMatcher.ExtractGenericInterface(modelType, typeof(ICollection<>));
        if (collectionType != null)
        {
            return CreateInstance(context, collectionType);
        }

        // If the model type is IEnumerable<> then we need to know if we can assign a List<> to it, since
        // that's what we would create. (The cases handled here are IEnumerable<>, IReadOnlyCollection<> and
        // IReadOnlyList<>).
        var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(modelType, typeof(IEnumerable<>));
        if (enumerableType != null)
        {
            var listType = typeof(List<>).MakeGenericType(enumerableType.GenericTypeArguments);
            if (modelType.IsAssignableFrom(listType))
            {
                return CreateInstance(context, listType);
            }
        }

        return null;
    }

    private static IModelBinder CreateInstance(ModelBinderProviderContext context, Type collectionType)
    {
        var binderType = typeof(CollectionModelBinder<>).MakeGenericType(collectionType.GenericTypeArguments);
        var elementType = collectionType.GenericTypeArguments[0];
        var elementBinder = context.CreateBinder(context.MetadataProvider.GetMetadataForType(elementType));

        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var mvcOptions = context.Services.GetRequiredService<IOptions<MvcOptions>>().Value;
        var binder = (IModelBinder)Activator.CreateInstance(
            binderType,
            elementBinder,
            loggerFactory,
            true /* allowValidatingTopLevelNodes */,
            mvcOptions)!;

        return binder;
    }
}
