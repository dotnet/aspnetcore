// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using MvcFormSample.Models;

namespace MvcFormSample.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index([FromQuery] bool antiforgery = true)
    {
        ViewBag.EnableAntiforgery = antiforgery;
        return View();
    }

    public IActionResult Index2([FromQuery] bool antiforgery = true)
    {
        ViewBag.EnableAntiforgery = antiforgery;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Index(Todo todo)
    {
        return View(todo);
    }

    [HttpPost]
    [RequireAntiforgeryToken]
    public ActionResult Index2(Todo todo)
    {
        return View(todo);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
