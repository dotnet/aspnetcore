// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using TagHelperSample.Web.Models;

namespace TagHelperSample.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly IEnumerable<SelectListItem> _items = new SelectList(Enumerable.Range(7, 13));
        private static readonly Dictionary<int, User> _users = new Dictionary<int, User>();
        private static int _next;

        public HomeController()
        {
            // Unable to set ViewBag (or ViewData) entries from constructor due to an InvalidOperationException thrown
            // from the DynamicViewData.ViewData getter. In MVC 5.2, no properties in the Controller class except
            // ControllerContext, Url, and anything ControllerContext-derived (e.g. HttpContext and User) return null
            // even if invoked from the constructor i.e. prior to the Initialize() call.
            ////ViewBag.Items = _items;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View(new List<User>(_users.Values));
        }

        // GET: /Home/Create
        public IActionResult Create()
        {
            ViewBag.Items = _items;
            return View();
        }

        // POST: Home/Create
        [HttpPost]
        public IActionResult Create(User user)
        {
            if (user != null && ModelState.IsValid)
            {
                var id = _next++;
                user.Id = id;
                _users[id] = user;
                return RedirectToAction("Index");
            }

            ViewBag.Items = _items;
            return View();
        }

        // GET: /Home/Edit/5
        public IActionResult Edit(int id)
        {
            User user;
            _users.TryGetValue(id, out user);

            ViewBag.Items = _items;
            return View(user);
        }

        // POST: Home/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, User user)
        {
            if (user != null && id == user.Id && _users.ContainsKey(id) && ModelState.IsValid)
            {
                _users[id] = user;
                return RedirectToAction("Index");
            }

            ViewBag.Items = _items;
            return View();
        }
    }
}
