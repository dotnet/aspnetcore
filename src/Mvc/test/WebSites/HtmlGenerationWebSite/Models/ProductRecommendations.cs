// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace HtmlGenerationWebSite.Models
{
    public class ProductRecommendations
    {
        public ProductRecommendations(params Product[] products)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            Products = products;
        }

        public IEnumerable<Product> Products { get; }
    }
}
