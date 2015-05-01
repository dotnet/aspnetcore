// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    [Route("FromFormAttribute_Company/[action]")]
    public class FromFormAttribute_CompanyController : Controller
    {
        public Company CreateCompany([FromForm(Name = "customPrefix")] Company company)
        {
            return company;
        }

        public FromForm_Department CreateDepartment(FromForm_Department department)
        {
            return department;
        }

        public Company CreateCompanyFromEmployees([FromForm(Name = "customPrefix")] IList<Employee> employees)
        {
            return new Company { Employees = employees };
        }

        public object ValidateDepartment(FromForm_Department department)
        {
            return new Result()
            {
                Value = department.Employees,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        public class FromForm_Department
        {
            [FromForm(Name = "TestEmployees")]
            [Required]
            public IEnumerable<Employee> Employees { get; set; }
        }
    }
}