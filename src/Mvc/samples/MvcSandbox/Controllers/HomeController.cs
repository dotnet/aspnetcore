// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    public class HomeController : Controller
    {
        [ModelBinder]
        public string Id { get; set; }

        public IActionResult Index()
        {
            return Ok(new Person { Name = "Test", Age = 10 });
        }
    }

    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    [JsonSerializable(typeof(Person))]
    public partial class MyJsonContext : JsonSerializerContext { }
}
