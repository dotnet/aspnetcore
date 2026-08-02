// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for complex types.
/// </summary>
[Obsolete("This type is obsolete and will be removed in a future version. Use ComplexObjectModelBinderProvider instead.")]
public class ComplexTypeModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.IsComplexType && !context.Metadata.IsCollectionType)
        {
            var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
            for (var i = 0; i < context.Metadata.Properties.Count; i++)
            {
                var property = context.Metadata.Properties[i];
                propertyBinders.Add(property, context.CreateBinder(property));
            }

            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new ComplexTypeModelBinder(
                propertyBinders,
                loggerFactory,
                allowValidatingTopLevelNodes: true);
        }

        return null;
    }
}
