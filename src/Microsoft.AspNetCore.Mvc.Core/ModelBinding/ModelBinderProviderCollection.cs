// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Represents a collection of model binder providers.
    /// </summary>
    public class ModelBinderProviderCollection : Collection<IModelBinderProvider>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBinderProviderCollection"/> class that is empty.
        /// </summary>
        public ModelBinderProviderCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBinderProviderCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="modelBinderProviders">The list that is wrapped by the new collection.</param>
        public ModelBinderProviderCollection(IList<IModelBinderProvider> modelBinderProviders)
            : base(modelBinderProviders)
        {
        }

        /// <summary>
        /// Removes all model binder providers of the specified type.
        /// </summary>
        /// <typeparam name="TModelBinderProvider">The type to remove.</typeparam>
        public void RemoveType<TModelBinderProvider>() where TModelBinderProvider : IModelBinderProvider
        {
            RemoveType(typeof(TModelBinderProvider));
        }

        /// <summary>
        /// Removes all model binder providers of the specified type.
        /// </summary>
        /// <param name="modelBinderProviderType">The type to remove.</param>
        public void RemoveType(Type modelBinderProviderType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var modelBinderProvider = this[i];
                if (modelBinderProvider.GetType() == modelBinderProviderType)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
