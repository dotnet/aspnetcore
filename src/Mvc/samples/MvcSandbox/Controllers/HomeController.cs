// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    public class HomeController : Controller
    {
        [ModelBinder]
        public string Id { get; set; }

        [Consumes("multipart/form-data")]
        public IActionResult Index([FromMultipartSection] Person person, IFormFileCollection forms)
        {
            return Ok(person.Age + forms.Count);
        }
    }

    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}
