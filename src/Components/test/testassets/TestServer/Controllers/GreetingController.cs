// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers
{
    [Route("api/[controller]/[action]")]
    public class GreetingController : Controller
    {
        [HttpGet]
        public string SayHello() => "Hello";
    }
}
