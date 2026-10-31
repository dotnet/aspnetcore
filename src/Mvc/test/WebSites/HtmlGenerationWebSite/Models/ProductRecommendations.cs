// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlGenerationWebSite.Models;

public class ProductRecommendations
{
    public ProductRecommendations(params Product[] products)
    {
        ArgumentNullException.ThrowIfNull(products);

        Products = products;
    }

    public IEnumerable<Product> Products { get; }
}
