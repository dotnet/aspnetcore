// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace HtmlGenerationWebSite.Models
{
    public class Folder
    {
        public IFormFile InterfaceFile { get; set; }

        public IFormFileCollection InterfaceFiles { get; set; }

        public FormFile ConcreteFile { get; set; }

        public FormFileCollection ConcreteFiles { get; set; }

        public IEnumerable<IFormFile> EnumerableFiles { get; set; }
    }
}