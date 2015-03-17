// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A context for <see cref="IModelValidatorProvider"/>.
    /// </summary>
    public class ModelValidatorProviderContext
    {
        /// <summary>
        /// Creates a new <see cref="ModelValidatorProviderContext"/>.
        /// </summary>
        /// <param name="modelMetadata">The <see cref="ModelBinding.ModelMetadata"/>.</param>
        public ModelValidatorProviderContext(ModelMetadata modelMetadata)
        {
            ModelMetadata = modelMetadata;
        }

        /// <summary>
        /// Gets the <see cref="ModelBinding.ModelMetadata"/>.
        /// </summary>
        public ModelMetadata ModelMetadata { get; }

        /// <summary>
        /// Gets the validator metadata.
        /// </summary>
        /// <remarks>
        /// This property provides convenience access to <see cref="ModelMetadata.ValidatorMetadata"/>.
        /// </remarks>
        public IReadOnlyList<object> ValidatorMetadata
        {
            get
            {
                return ModelMetadata.ValidatorMetadata;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IModelValidator"/> instances. <see cref="IModelValidatorProvider"/> instances
        /// should add validators to this list when
        /// <see cref="IModelValidatorProvider.GetValidators(ModelValidatorProviderContext)"/>
        /// is called.
        /// </summary>
        public IList<IModelValidator> Validators { get; } = new List<IModelValidator>();
    }
}