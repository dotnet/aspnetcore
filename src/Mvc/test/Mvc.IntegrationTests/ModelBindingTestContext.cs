// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ModelBindingTestContext : ControllerContext
    {
        public IModelMetadataProvider MetadataProvider { get; set; }

        public MvcOptions MvcOptions { get; set; }

        public T GetService<T>()
        {
            return (T)HttpContext.RequestServices.GetService(typeof(T));
        }
    }
}
