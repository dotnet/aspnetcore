// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    [Route("FromQueryAttribute_Company/[action]")]
    public class FromQueryAttribute_CompanyController : Controller
    {
        public Company CreateCompany([FromQuery(Name = "customPrefix")] Company company)
        {
            return company;
        }

        public Company CreateCompanyFromEmployees([FromQuery(Name = "customPrefix")] IList<Employee> employees)
        {
            return new Company { Employees = employees };
        }

        public FromQuery_Department CreateDepartment(FromQuery_Department department)
        {
            return department;
        }

        public object ValidateDepartment(FromQuery_Department department)
        {
            return new Result()
            {
                Value = department.Employees,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        public class FromQuery_Department
        {
            [FromQuery(Name = "TestEmployees")]
            [Required]
            public IEnumerable<Employee> Employees { get; set; }
        }
    }
}