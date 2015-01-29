// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FormatterWebSite
{
    public class TryValidateModelController : Controller
    {
        [HttpGet]
        public IActionResult GetInvalidUser()
        {
            var user = new User
            {
                Id = 0,
                Name = "x"
            };

            // If ModelState.InValid is false return BadRequestOjectResult; else return empty string.
            if (!TryValidateModel(user))
            {
                return new BadRequestObjectResult(ModelState);
            }

            return Content(string.Empty);
        }

        [HttpGet]
        public IActionResult GetInvalidAdminWithPrefix()
        {
            var admin = new Administrator()
            {
                Id = 1,
                Name = "John Doe",
                Designation = "Administrator",
                AdminAccessCode = 0
            };
            if (!TryValidateModel(admin,"admin"))
            {
                return new BadRequestObjectResult(ModelState);
            }

            return Content(string.Empty);
        }

        [HttpGet]
        public IActionResult GetValidAdminWithPrefix()
        {
            var admin = new Administrator()
            {
                Id = 1,
                Name = "John Doe",
                Designation = "Administrator",
                AdminAccessCode = 1
            };
            if (!TryValidateModel(admin, "admin"))
            {
                return new BadRequestObjectResult(ModelState);
            }

            return Content("Admin user created successfully");
        }
    }
}
