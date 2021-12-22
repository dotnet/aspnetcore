// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers;

[Route("api/[controller]/[action]")]
public class GreetingController : Controller
{
    [HttpGet]
    public string SayHello() => "Hello";
}
