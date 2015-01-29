// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite
{
    public class User_FromBody
    {
        [FromRoute]
        public Address HomeAddress { get; set; }

        [FromBody]
        public Address OfficeAddress { get; set; }

        [FromQuery]
        public Address ShippingAddress { get; set; }

        // Should get it from the first value provider which
        // can provide values for this.
        public Address DefaultAddress { get; set; }
    }
}