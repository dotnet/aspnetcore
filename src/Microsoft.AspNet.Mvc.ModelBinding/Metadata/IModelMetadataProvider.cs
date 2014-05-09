// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IModelMetadataProvider
    {
        IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType);

        ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName);

        ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType);
    }
}
