// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for arrays.
    /// </summary>
    public class ArrayModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType.IsArray)
            {
                var elementType = context.Metadata.ElementMetadata.ModelType;
                var elementBinder = context.CreateBinder(context.Metadata.ElementMetadata);

                var binderType = typeof(ArrayModelBinder<>).MakeGenericType(elementType);
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return (IModelBinder)Activator.CreateInstance(
                    binderType,
                    elementBinder,
                    loggerFactory,
                    true /* allowValidatingTopLevelNodes */);
            }

            return null;
        }
    }
}
