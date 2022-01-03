// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using TagHelpersWebSite.Models;

namespace TagHelpersWebSite.Controllers;

public class EmployeeController : Controller
{
    // GET: Employee
    public string Index()
    {
        return "Index Page";
    }

    // GET: Employee/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Employee/Create
    [HttpPost]
    public IActionResult Create(Employee employee)
    {
        if (ModelState.IsValid)
        {
            return View("Details", employee);
        }
        return View(employee);
    }

    // GET: Employee/DuplicateAntiforgeryTokenRegistration
    public IActionResult DuplicateAntiforgeryTokenRegistration()
    {
        return View();
    }
}
