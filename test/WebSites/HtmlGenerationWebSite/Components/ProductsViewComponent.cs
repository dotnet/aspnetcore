// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Caching;
using Microsoft.Framework.Caching.Memory;

namespace HtmlGenerationWebSite.Components
{
    public class ProductsViewComponent : ViewComponent
    {
        public ProductsViewComponent(ProductsService productsService, IMemoryCache cache)
        {
            ProductsService = productsService;
            Cache = cache;
        }

        private ProductsService ProductsService { get; }

        public IMemoryCache Cache { get; }

        public IViewComponentResult Invoke(string category)
        {
            string products;
            if (!Cache.TryGetValue(category, out products))
            {
                IExpirationTrigger trigger;
                products = ProductsService.GetProducts(category, out trigger);
                Cache.Set(category, products, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));
            }

            ViewData["Products"] = products;
            return View();
        }
    }
}