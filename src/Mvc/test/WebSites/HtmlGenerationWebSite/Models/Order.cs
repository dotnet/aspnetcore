// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace HtmlGenerationWebSite.Models
{
    public class Order
    {
        public bool NeedSpecialHandle
        {
            get;
            set;
        }

        public DateTimeOffset OrderDate
        {
            get;
            set;
        }

        public ICollection<string> PaymentMethod
        {
            get;
            set;
        }

        public DateTime ShippingDateTime
        {
            get;
            set;
        }

        public string Shipping
        {
            get;
            set;
        }

        public IEnumerable<int> Products
        {
            get;
            set;
        }

        public IEnumerable<int> SubstituteProducts
        {
            get;
            set;
        }

        public Customer Customer
        {
            get;
            set;
        }

        public IList<Product> ProductDetails { get; } = new List<Product>();
    }
}