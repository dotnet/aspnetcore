// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Models
{
    public class Resident : Person
    {
        public IEnumerable<Address> ShippingAddresses { get; set; }

        public Address HomeAddress { get; set; }

        [FromBody]
        public Address OfficeAddress { get; set; }
    }
}