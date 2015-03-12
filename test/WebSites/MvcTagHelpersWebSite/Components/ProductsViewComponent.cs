// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Expiration.Interfaces;

namespace MvcTagHelpersWebSite.Components
{
    public class ProductsViewComponent : ViewComponent
    {
        [Activate]
        public ProductsService ProductsService { get; set; }

        public IViewComponentResult Invoke(string category)
        {
            IExpirationTrigger trigger;
            var products = ProductsService.GetProducts(category, out trigger);
            EntryLinkHelpers.ContextLink.AddExpirationTriggers(new[] { trigger });

            ViewData["Products"] = products;
            return View();
        }
    }
}