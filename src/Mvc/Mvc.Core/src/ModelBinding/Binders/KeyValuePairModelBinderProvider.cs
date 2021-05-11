// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    public class KeyValuePairModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
}
