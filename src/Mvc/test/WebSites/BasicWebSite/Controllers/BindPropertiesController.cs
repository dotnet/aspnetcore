// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite
{
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
}
