// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite
{
    public class Resident : Person
    {
        public IEnumerable<Address> ShippingAddresses { get; set; }

        public Address HomeAddress { get; set; }

        [FromBody]
        public Address OfficeAddress { get; set; }
    }
}