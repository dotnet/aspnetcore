// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    [Route("FromRouteAttribute_Company/[action]/{customPrefix.Name}")]
    public class FromRouteAttribute_CompanyController : Controller
    {
        [HttpGet("{customPrefix.EmployeeTaxId?}")]
        public Employee CreateEmployee([FromRoute(Name = "customPrefix")] Employee employee)
        {
            return employee;
        }

        public Company CreateCompanyFromEmployees([FromRoute(Name = "customPrefix")] IList<Employee> employees)
        {
            return new Company { Employees = employees };
        }

        public object ValidateDepartment(FromRoute_Department department)
        {
            return new Result()
            {
                Value = department.Employees,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        public class FromRoute_Department
        {
            [FromRoute(Name = "TestEmployees")]
            [Required]
            public IEnumerable<Employee> Employees { get; set; }
        }
    }
}