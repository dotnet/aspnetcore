// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    [Route("ModelBinderAttribute_Company/[action]")]
    public class ModelBinderAttribute_CompanyController : Controller
    {
        // Uses Name to set a custom prefix
        public Company GetCompany([ModelBinder(Name = "customPrefix")] Company company)
        {
            return company;
        }

        public Company CreateCompany(IList<Employee> employees)
        {
            return new Company { Employees = employees };
        }
    }
}