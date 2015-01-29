// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IModelMetadataProvider
    {
        IEnumerable<ModelMetadata> GetMetadataForProperties(object container, [NotNull] Type containerType);

        ModelMetadata GetMetadataForProperty(
            Func<object> modelAccessor,
            [NotNull] Type containerType,
            [NotNull] string propertyName);

        ModelMetadata GetMetadataForType(Func<object> modelAccessor, [NotNull] Type modelType);

        ModelMetadata GetMetadataForParameter(
            Func<object> modelAccessor,
            [NotNull] MethodInfo methodInfo,
            [NotNull] string parameterName);
    }
}
