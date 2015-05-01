// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace ModelBindingWebSite.Models
{
    public class Book
    {
        public string Name { get; set; }

        public IFormFile File { get; set; }
    }
}