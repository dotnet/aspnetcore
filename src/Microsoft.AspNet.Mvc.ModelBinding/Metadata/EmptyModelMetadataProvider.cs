// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelMetadataProvider"/> that provides base <see cref="ModelMetadata"/> instances and does not
    /// set most <see cref="ModelMetadata"/> properties. For example this provider does not use data annotations.
    /// </summary>
    /// <remarks>
    /// Provided for efficiency in scenarios that require minimal <see cref="ModelMetadata"/> information.
    /// </remarks>
    public class EmptyModelMetadataProvider : AssociatedMetadataProvider<ModelMetadata>
    {
        /// <inheritdoc />
        /// <remarks>Ignores <paramref name="attributes"/>.</remarks>
        protected override ModelMetadata CreateMetadataPrototype(IEnumerable<object> attributes,
                                                                 Type containerType,
                                                                 [NotNull] Type modelType,
                                                                 string propertyName)
        {
            return new ModelMetadata(
                this,
                containerType,
                modelAccessor: null,
                modelType: modelType,
                propertyName: propertyName);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Copies very few values from the <paramref name="prototype"/>. Likely <paramref name="prototype"/> has not
        /// been modified except to add <see cref="ModelMetadata.AdditionalValues"/> entries.
        /// </remarks>
        protected override ModelMetadata CreateMetadataFromPrototype([NotNull] ModelMetadata prototype,
                                                                     Func<object> modelAccessor)
        {
            var metadata = new ModelMetadata(
                this,
                prototype.ContainerType,
                modelAccessor,
                prototype.ModelType,
                prototype.PropertyName);
            foreach (var keyValuePair in prototype.AdditionalValues)
            {
                metadata.AdditionalValues.Add(keyValuePair);
            }

            return metadata;
        }
    }
}
