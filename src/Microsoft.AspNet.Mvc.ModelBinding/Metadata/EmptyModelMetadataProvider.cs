// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class EmptyModelMetadataProvider : AssociatedMetadataProvider<ModelMetadata>
    {
        protected override ModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes,
                                                                 Type containerType,
                                                                 Type modelType,
                                                                 string propertyName)
        {
            return new ModelMetadata(this, containerType, null, modelType, propertyName);
        }

        protected override ModelMetadata CreateMetadataFromPrototype(ModelMetadata prototype,
                                                                     Func<object> modelAccessor)
        {
            return new ModelMetadata(this,
                                     prototype.ContainerType,
                                     modelAccessor,
                                     prototype.ModelType,
                                     prototype.PropertyName);
        }
    }
}
