// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using HtmlGenerationWebSite.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HtmlGenerationWebSite
{
    public class ProductsService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ISignalTokenProviderService<Product> _tokenProviderService;
        private readonly Dictionary<string, Product[]> _products = new Dictionary<string, Product[]>
        {
            ["Books"] = new[]
            {
                new Product { ProductName = "Book1" },
                new Product { ProductName = "Book2" }
            },
            ["Electronics"] = new[]
            {
                new Product { ProductName = "Laptops" }
            }
        };

        public ProductsService(
            IMemoryCache memoryCache,
            ISignalTokenProviderService<Product> tokenProviderService)
        {
            _memoryCache = memoryCache;
            _tokenProviderService = tokenProviderService;
        }

        public IEnumerable<string> GetProductNames(string category)
        {
            IEnumerable<Product> products;
            var key = typeof(ProductsService).FullName;
            if (!_memoryCache.TryGetValue(key, out products))
            {
                var changeToken = _tokenProviderService.GetToken(key);
                products = _memoryCache.Set<IEnumerable<Product>>(
                    key,
                    _products[category],
                    new MemoryCacheEntryOptions().AddExpirationToken(changeToken));
            }

            return products.Select(p => p.ProductName);
        }

        public void UpdateProducts(string category, IEnumerable<Product> products)
        {
            _products[category] = products.ToArray();
            var key = typeof(ProductsService).FullName;
            _tokenProviderService.SignalToken(key);
        }
    }
}