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
    /// An <see cref="IModelBinderProvider"/> for complex types.
    /// </summary>
    public class ComplexObjectModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var metadata = context.Metadata;
            if (metadata.IsComplexType && !metadata.IsCollectionType)
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<ComplexObjectModelBinder>();
                var parameterBinders = GetParameterBinders(context);

                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                for (var i = 0; i < context.Metadata.Properties.Count; i++)
                {
                    var property = context.Metadata.Properties[i];
                    propertyBinders.Add(property, context.CreateBinder(property));
                }

                return new ComplexObjectModelBinder(propertyBinders, parameterBinders, logger);
            }

            return null;
        }

        private static IReadOnlyList<IModelBinder> GetParameterBinders(ModelBinderProviderContext context)
        {
            var boundConstructor = context.Metadata.BoundConstructor;
            if (boundConstructor is null)
            {
                return Array.Empty<IModelBinder>();
            }

            var parameterBinders = boundConstructor.BoundConstructorParameters!.Count == 0 ?
                Array.Empty<IModelBinder>() :
                new IModelBinder[boundConstructor.BoundConstructorParameters.Count];

            for (var i = 0; i < parameterBinders.Length; i++)
            {
                parameterBinders[i] = context.CreateBinder(boundConstructor.BoundConstructorParameters[i]);
            }

            return parameterBinders;
        }
    }
}
