// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Controllers
{
    public class FormUrlEncodedController : Controller
    {
        [Route("[controller]")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("[controller]/[action]")]
        public bool IsValidPerson(Person person)
        {
            return ModelState.IsValid && person.PastJobs.Any();
        }
    }
}