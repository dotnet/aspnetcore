// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace RazorInstrumentationWebSite
{
    public class HomeController : Controller
    {
        public ActionResult FullPath()
        {
            return View("/Views/Home/FullPath.cshtml");
        }

        public ActionResult ViewDiscoveryPath()
        {
            return View();
        }

        public ActionResult ViewWithPartial()
        {
            return View();
        }
    }
}