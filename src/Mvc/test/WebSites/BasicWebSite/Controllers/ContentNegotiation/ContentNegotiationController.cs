// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class ContentNegotiationController : Controller
{
    public IActionResult Index()
    {
        return new JsonResult("Index Method");
    }

    public User UserInfo()
    {
        return CreateUser();
    }

    [Produces<User>]
    public IActionResult UserInfo_ProducesWithTypeOnly()
    {
        return new ObjectResult(CreateUser());
    }

    [Produces<User>]
    public IActionResult UserInfo_ProducesWithTypeAndContentType()
    {
        return new ObjectResult(CreateUser());
    }

    private User CreateUser()
    {
        return new User() { Name = "John", Address = "One Microsoft Way" };
    }
}
