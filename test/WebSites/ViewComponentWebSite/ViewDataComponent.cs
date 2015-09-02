// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    [ViewComponent(Name = "ViewData")]
    public class ViewDataComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            ViewData["value-from-component"] = nameof(Invoke) + ": hello from viewdatacomponent";
            return View("ComponentThatReadsViewData");
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            ViewData["value-from-component"] = nameof(InvokeAsync) + ": hello from viewdatacomponent";
            var result = View("ComponentThatReadsViewData");
            return Task.FromResult<IViewComponentResult>(result);
        }
    }
}