// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace JsonPatchSample.Web.Models
{
    public class Order
    {
        public string OrderName { get; set; }

        [JsonConverter(typeof(ReplaceOrderTypeConverter))]
        public string OrderType { get; set; }
    }
}