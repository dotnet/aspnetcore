// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Models
{
    public class User_FromForm
    {
        [FromRoute]
        public Address HomeAddress { get; set; }

        [FromForm]
        public Address OfficeAddress { get; set; }

        [FromQuery]
        public Address ShippingAddress { get; set; }
    }
}