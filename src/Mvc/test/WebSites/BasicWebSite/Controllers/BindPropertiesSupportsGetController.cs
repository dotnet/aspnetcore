// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite
{
    [BindProperties(SupportsGet = true)]
    public class BindPropertiesSupportsGetController : Controller
    {
        public string Name { get; set; }

        public IActionResult Action() => Content(Name);
    }
}
