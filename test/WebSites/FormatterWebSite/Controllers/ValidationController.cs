// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}