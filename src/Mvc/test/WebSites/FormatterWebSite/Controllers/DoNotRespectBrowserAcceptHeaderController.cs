// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
    public class DoNotRespectBrowserAcceptHeaderController : Controller
    {
        [HttpGet]
        public Employee EmployeeInfo()
        {
            return new Employee()
            {
                Id = 10,
                Name = "John"
            };
        }

        [HttpGet]
        [Produces("application/xml")]
        public Employee EmployeeInfoWithProduces()
        {
            return new Employee()
            {
                Id = 20,
                Name = "Mike"
            };
        }

        [HttpPost]
        public IActionResult CreateEmployee([FromBody]Employee employee)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return new ObjectResult(employee);
        }

        public class Employee
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
