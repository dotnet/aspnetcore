// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelMetadataProvider"/> implementation that provides
    /// <see cref="CachedDataAnnotationsModelMetadata"/> instances. Those instances primarily calculate property values
    /// using attributes from the <see cref="System.ComponentModel.DataAnnotations"/> namespace.
    /// </summary>
    public class DataAnnotationsModelMetadataProvider : AssociatedMetadataProvider<CachedDataAnnotationsModelMetadata>
    {
        /// <inheritdoc />
        protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(
            IEnumerable<object> attributes,
            Type containerType,
            Type modelType,
            string propertyName)
        {
            return new CachedDataAnnotationsModelMetadata(this, containerType, modelType, propertyName, attributes);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Copies only a few values from the <paramref name="prototype"/>. Unlikely the rest have been computed.
        /// </remarks>
        protected override CachedDataAnnotationsModelMetadata CreateMetadataFromPrototype(
            CachedDataAnnotationsModelMetadata prototype)
        {
            var metadata = new CachedDataAnnotationsModelMetadata(prototype);
            foreach (var keyValuePair in prototype.AdditionalValues)
            {
                metadata.AdditionalValues.Add(keyValuePair);
            }

            return metadata;
        }
    }
}
