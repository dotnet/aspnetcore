// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TagHelpersWebSite.Models;

namespace TagHelpersWebSite.Controllers
{
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
}