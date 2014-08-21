// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite
{
    public class FlushPoint : Controller
    {
        public ViewResult PageWithLayout()
        {
            return View();
        }

        public ViewResult PageWithoutLayout()
        {
            return View();
        }

        public ViewResult PageWithPartialsAndViewComponents()
        {
            return View();
        }
    }
}