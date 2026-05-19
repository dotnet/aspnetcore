// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite;

[BindProperties]
public class BindPropertiesController : Controller
{
    public string Name { get; set; }

    public int? Id { get; set; }

    [FromRoute]
    public int? IdFromRoute { get; set; }

    [ModelBinder(typeof(CustomBoundModelBinder))]
    public string CustomBound { get; set; }

    [BindNever]
    public string BindNeverProperty { get; set; }

    public object Action() => new { Name, Id, IdFromRoute, CustomBound, BindNeverProperty };

    private class CustomBoundModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success("CustomBoundValue");
            return Task.CompletedTask;
        }
    }
}
