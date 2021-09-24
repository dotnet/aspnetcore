// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
