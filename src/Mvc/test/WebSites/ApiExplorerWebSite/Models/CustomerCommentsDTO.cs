// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    public class CustomerCommentsDTO
    {
        [FromQuery]
        public string ShippingInstructions { get; set; }

        public string Feedback { get; set; }
    }
}