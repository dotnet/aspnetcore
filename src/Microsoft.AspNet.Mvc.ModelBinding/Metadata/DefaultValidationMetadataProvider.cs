// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="IValidationMetadataProvider"/>.
    /// </summary>
    public class DefaultValidationMetadataProvider : IValidationMetadataProvider
    {
        /// <inheritdoc />
        public void GetValidationMetadata([NotNull] ValidationMetadataProviderContext context)
        {
            foreach (var attribute in context.Attributes.OfType<IModelValidator>())
            {
                context.ValidationMetadata.ValiatorMetadata.Add(attribute);
            }
        }
    }
}