// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System.Collections.Generic;

namespace MvcSample.Web.ApiExplorerSamples
{
    public class Order
    {
        [FromRoute]
        public int AccountId { get; set; }

        [FromBody]
        public List<OrderItem> Items { get; set; }

        [FromQuery]
        public bool? IncludeWarranty { get; set; }

        public class OrderItem
        {
            public int ProductId { get; set; }

            public int Quantity { get; set; }
        }
    }
}