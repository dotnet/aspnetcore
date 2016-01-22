// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace CustomRouteSample.Web.Controllers.Spain
{
    [Locale("es-ES")]
    public class ProductsController : Controller
    {
        public string Index()
        {
            return "Hola from Spain.";
        }
    }
}