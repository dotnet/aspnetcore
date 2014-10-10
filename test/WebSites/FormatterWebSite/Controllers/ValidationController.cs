// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace FormatterWebSite
{
    public class ValidationController : Controller
    {
        [HttpPost]
        public IActionResult Index([FromBody]User user)
        {
            if (!ModelState.IsValid)
            {
                return Content(ModelState["user.Id"].Errors[0].ErrorMessage + "," +
                    ModelState["user.Name"].Errors[0].ErrorMessage + "," +
                    ModelState["user.Alias"].Errors[0].ErrorMessage + "," +
                    ModelState["user.Designation"].Errors[0].ErrorMessage);
            }

            return Content("User has been registerd : " + user.Name);
        }

        [HttpPost]
        public string GetDeveloperName([FromBody]Developer developer)
        {
            if (ModelState.IsValid)
            {
                return "Developer's get was not accessed after set.";
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}