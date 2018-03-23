// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
