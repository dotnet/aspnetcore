// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.ApiExplorerSamples
{
    public class UpdateProductDTO
    {
        public int Id { get; set; }

        [FromBody]
        public Product Product { get; set; }

        [FromHeader(Name = "Admin-User")]
        public string AdminId { get; set; }

        public string Comments { get; set; }
    }
}