// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class ModelBindingTestContext : ControllerContext
{
    public IModelMetadataProvider MetadataProvider { get; set; }

    public MvcOptions MvcOptions { get; set; }

    public T GetService<T>()
    {
        return (T)HttpContext.RequestServices.GetService(typeof(T));
    }
}
