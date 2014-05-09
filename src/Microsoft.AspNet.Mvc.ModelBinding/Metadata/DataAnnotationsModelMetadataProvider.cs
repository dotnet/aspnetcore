// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelMetadataProvider : AssociatedMetadataProvider<CachedDataAnnotationsModelMetadata>
    {
        protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(
            IEnumerable<Attribute> attributes, 
            Type containerType, 
            Type modelType, 
            string propertyName)
        {
            return new CachedDataAnnotationsModelMetadata(this, containerType, modelType, propertyName, attributes);
        }

        protected override CachedDataAnnotationsModelMetadata CreateMetadataFromPrototype(
            CachedDataAnnotationsModelMetadata prototype, 
            Func<object> modelAccessor)
        {
            return new CachedDataAnnotationsModelMetadata(prototype, modelAccessor);
        }
    }
}
