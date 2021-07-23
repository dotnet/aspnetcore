// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BasicViews.Controllers
{
    public class HomeController : Controller
    {
        private readonly BasicViewsContext _context;

        public HomeController(BasicViewsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Person person)
        {
            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
            }

            return View(person);
        }

        [HttpGet]
        public IActionResult IndexWithoutToken()
        {
            return View(viewName: nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IndexWithoutToken(Person person)
        {
            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
            }

            return View(viewName: nameof(Index), model: person);
        }

        [HttpGet]
        public IActionResult HtmlHelpers()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HtmlHelpers(Person person)
        {
            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
            }

            return View(person);
        }
    }
}
