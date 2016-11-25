// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for complex types.
    /// </summary>
    public class ComplexTypeModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.IsComplexType &&
                !context.Metadata.IsCollectionType &&
                HasDefaultConstructor(context.Metadata.ModelType.GetTypeInfo()))
            {
                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                for (var i = 0; i < context.Metadata.Properties.Count; i++)
                {
                    var property = context.Metadata.Properties[i];
                    propertyBinders.Add(property, context.CreateBinder(property));
                }

                return new ComplexTypeModelBinder(propertyBinders);
            }

            return null;
        }

        private bool HasDefaultConstructor(TypeInfo modelTypeInfo)
        {
            // The following check causes the ComplexTypeModelBinder to NOT participate in binding structs.
            // - Reflection does not provide information about the implicit parameterless constructor for a struct.
            // - Also this binder would eventually fail to construct an instance of the struct as the Linq's
            //   NewExpression compile fails to construct it.
            return !modelTypeInfo.IsAbstract && modelTypeInfo.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
