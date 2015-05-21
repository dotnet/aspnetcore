// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApiExplorer;

namespace MvcSample.Web
{
    [Route("ApiExplorer")]
    public class ApiExplorerController : Controller
    {
        public ApiExplorerController(IApiDescriptionGroupCollectionProvider provider)
        {
            Provider = provider;
        }

        public IApiDescriptionGroupCollectionProvider Provider { get; }

        [HttpGet]
        public IActionResult All()
        {
            var descriptions = Provider.ApiDescriptionGroups.Items;
            return View(descriptions);
        }
    }
}