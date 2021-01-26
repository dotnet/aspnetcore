// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An <see cref="IModelValidatorProvider" /> that provides <see cref="IModelValidator"/> instances
    /// exclusively using values in <see cref="ModelMetadata.ValidatorMetadata"/> or the model type.
    /// <para>
    /// <see cref="IMetadataBasedModelValidatorProvider" /> can be used to statically determine if a given
    /// <see cref="ModelMetadata"/> instance can incur any validation. The value for <see cref="ModelMetadata.HasValidators"/>
    /// can be calculated if all instances in <see cref="MvcOptions.ModelValidatorProviders"/> are <see cref="IMetadataBasedModelValidatorProvider" />.
    /// </para>
    /// </summary>
    public interface IMetadataBasedModelValidatorProvider : IModelValidatorProvider
    {
        /// <summary>
        /// Gets a value that determines if the <see cref="IModelValidatorProvider"/> can
        /// produce any validators given the <paramref name="modelType"/> and <paramref name="modelType"/>.
        /// </summary>
        /// <param name="modelType">The <see cref="Type"/> of the model.</param>
        /// <param name="validatorMetadata">The list of metadata items for validators. <seealso cref="ValidationMetadata.ValidatorMetadata"/>.</param>
        /// <returns></returns>
        bool HasValidators(Type modelType, IList<object> validatorMetadata);
    }
}
