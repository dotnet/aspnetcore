// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace FiltersWebSite
{
    [BlockAnonymous]
    public class AnonymousController : Controller
    {
        public string GetHelloWorld()
        {
            return "Hello World!";
        }
    }
}