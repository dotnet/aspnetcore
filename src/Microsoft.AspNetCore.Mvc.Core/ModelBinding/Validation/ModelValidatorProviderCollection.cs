// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Represents a collection of model validator providers.
    /// </summary>
    public class ModelValidatorProviderCollection : Collection<IModelValidatorProvider>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelValidatorProviderCollection"/> class that is empty.
        /// </summary>
        public ModelValidatorProviderCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelValidatorProviderCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="modelValidatorProviders">The list that is wrapped by the new collection.</param>
        public ModelValidatorProviderCollection(IList<IModelValidatorProvider> modelValidatorProviders)
            : base(modelValidatorProviders)
        {
        }

        /// <summary>
        /// Removes all model validator providers of the specified type.
        /// </summary>
        /// <typeparam name="TModelValidatorProvider">The type to remove.</typeparam>
        public void RemoveType<TModelValidatorProvider>() where TModelValidatorProvider : IModelValidatorProvider
        {
            RemoveType(typeof(TModelValidatorProvider));
        }

        /// <summary>
        /// Removes all model validator providers of the specified type.
        /// </summary>
        /// <param name="modelValidatorProviderType">The type to remove.</param>
        public void RemoveType(Type modelValidatorProviderType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var modelValidatorProvider = this[i];
                if (modelValidatorProvider.GetType() == modelValidatorProviderType)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
