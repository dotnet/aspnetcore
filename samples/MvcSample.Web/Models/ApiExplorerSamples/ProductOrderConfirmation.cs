// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MvcSample.Web.ApiExplorerSamples
{
    public class ProductOrderConfirmation
    {
        public Product Product { get; set; }

        public decimal PricePerUnit { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }
    }
}