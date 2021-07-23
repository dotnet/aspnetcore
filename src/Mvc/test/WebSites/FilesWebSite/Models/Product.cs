// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace FilesWebSite.Models
{
    public class Product
    {
        public string Name { get; set; }

        public IDictionary<string, IEnumerable<IFormFile>> Specs { get; set; }
    }
}
