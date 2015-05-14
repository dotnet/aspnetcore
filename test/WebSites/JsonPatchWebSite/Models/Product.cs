// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace JsonPatchWebSite.Models
{
    public class Product
    {
        public string ProductName { get; set; }

        public Category ProductCategory { get; set; }
    }

    [JsonConverter(typeof(ProductCategoryConverter))]
    public class Category
    {
        public string CategoryName { get; set; }
    }
}
