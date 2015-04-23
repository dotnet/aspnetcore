// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApiExplorer;

namespace MvcSample.Web
{
    [Route("ApiExplorer")]
    public class ApiExplorerController : Controller
    {
        [Activate]
        public IApiDescriptionGroupCollectionProvider Provider { get; set; }

        [HttpGet]
        public IActionResult All()
        {
            var descriptions = Provider.ApiDescriptionGroups.Items;
            return View(descriptions);
        }
    }
}