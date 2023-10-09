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

    public IActionResult Index3()
    {
        return View();
    }

    // Result: FormatException: Input string was not in a correct format.
    [HttpPost("TestCrash")]
    public IActionResult TestCrash(Dictionary<int, int> data)
    {
        return RedirectToAction("");
    }

    // Result: Works as expected
    [HttpPost("TestWorks")]
    public IActionResult TestWorks(List<int> data)
    {
        return RedirectToAction("Index");
    }

    // Result: Works as expected
    [HttpPost("TestWorks2")]
    public IActionResult TestWorks2(HashSet<int> data)
    {
        return RedirectToAction("Index");
    }

    // Result: Works as expected
    [HttpPost("TestWorks3")]
    public IActionResult TestWorks3(int[] data)
    {
        return RedirectToAction("Index");
    }

    // Result: FormatException: Input string was not in a correct format.
    [HttpPost("TestCrash2")]
    public IActionResult TestCrash2(Dictionary<long, string> data)
    {
        return RedirectToAction("");
    }

    // Result: Works as expected
    [HttpPost("TestWorks4")]
    public IActionResult TestWorks4(Dictionary<string, int> data)
    {
        return RedirectToAction("Index");
    }
}

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{

    [HttpGet]
    [Route("key")]
    public IActionResult KeyTest([FromQuery] Dictionary<TestEnum, string> prop)
    {
        return Ok();
    }

    [HttpGet]
    [Route("value")]
    public IActionResult ValueTest([FromQuery] Dictionary<string, TestEnum> prop)
    {
        return Ok();
    }
}

public enum TestEnum
{
    EnumVal1,
    EnumVal2
}
