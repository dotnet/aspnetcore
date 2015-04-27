// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A context for <see cref="IClientModelValidatorProvider"/>.
    /// </summary>
    public class ClientValidatorProviderContext
    {
        /// <summary>
        /// Creates a new <see cref="ClientValidatorProviderContext"/>.
        /// </summary>
        /// <param name="modelMetadata">The <see cref="ModelBinding.ModelMetadata"/> for the model being validated.
        /// </param>
        public ClientValidatorProviderContext(ModelMetadata modelMetadata)
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
        /// Gets the list of <see cref="IClientModelValidator"/> instances. <see cref="IClientModelValidatorProvider"/>
        /// instances should add validators to this list when
        /// <see cref="IClientModelValidatorProvider.GetValidators(ClientValidatorProviderContext)()"/>
        /// is called.
        /// </summary>
        public IList<IClientModelValidator> Validators { get; } = new List<IClientModelValidator>();
    }
}