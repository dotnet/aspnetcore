// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

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
    }
}