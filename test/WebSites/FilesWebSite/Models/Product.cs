// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
