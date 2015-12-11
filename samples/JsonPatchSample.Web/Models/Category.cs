// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace JsonPatchSample.Web.Models
{
    [JsonConverter(typeof(ProductCategoryConverter))]
    public class Category
    {
        public string CategoryName { get; set; }
    }
}
