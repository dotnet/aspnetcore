// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite.Controllers.ParameterBinding
{
    public class EmployeesController : ApiController
    {
        public IActionResult PostByIdDefault(int id = -1)
        {
            return Ok(id);
        }

        public IActionResult PostByIdModelBinder([ModelBinder] int id = -1)
        {
            return Ok(id);
        }

        public IActionResult PostByIdFromQuery([FromQuery] int id = -1)
        {
            return Ok(id);
        }

        public IActionResult PutEmployeeDefault(Employee employee)
        {
            return Ok(employee);
        }

        public IActionResult PutEmployeeModelBinder([ModelBinder] Employee employee)
        {
            return Ok(employee);
        }

        public IActionResult PutEmployeeBothDefault(string name, Employee employee)
        {
            employee.Name = name;
            return Ok(employee);
        }
    }
}