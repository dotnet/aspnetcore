// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MockModelBinderProvider : IModelBinderProvider
    {
        public List<IModelBinder> ModelBinders { get; set; } = new List<IModelBinder>();

        IReadOnlyList<IModelBinder> IModelBinderProvider.ModelBinders { get { return ModelBinders; } }
    }
}