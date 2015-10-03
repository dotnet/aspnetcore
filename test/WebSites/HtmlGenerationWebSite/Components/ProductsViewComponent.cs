// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

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
                IChangeToken changeToken;
                products = ProductsService.GetProducts(category, out changeToken);
                Cache.Set(category, products, new MemoryCacheEntryOptions().AddExpirationToken(changeToken));
            }

            ViewData["Products"] = products;
            return View();
        }
    }
}