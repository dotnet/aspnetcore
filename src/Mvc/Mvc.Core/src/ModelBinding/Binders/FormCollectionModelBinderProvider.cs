// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for <see cref="IFormCollection"/>.
    /// </summary>
    public class FormCollectionModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.ModelType;

            if (typeof(FormCollection).GetTypeInfo().IsAssignableFrom(modelType))
            {
                throw new InvalidOperationException(
                    Resources.FormatFormCollectionModelBinder_CannotBindToFormCollection(
                        typeof(FormCollectionModelBinder).FullName,
                        modelType.FullName,
                        typeof(IFormCollection).FullName));
            }

            if (modelType == typeof(IFormCollection))
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new FormCollectionModelBinder(loggerFactory);
            }

            return null;
        }
    }
}
