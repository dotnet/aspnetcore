// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IModelMetadataProvider
    {
        IEnumerable<ModelMetadata> GetMetadataForProperties([NotNull] Type containerType);

        ModelMetadata GetMetadataForType([NotNull] Type modelType);

        ModelMetadata GetMetadataForParameter([NotNull] MethodInfo methodInfo, [NotNull] string parameterName);
    }
}
