// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

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