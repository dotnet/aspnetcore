using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class HomeController : Controller
    {
        private static readonly IEnumerable<SelectListItem> _addresses = CreateAddresses();
        private static readonly IEnumerable<SelectListItem> _ages = CreateAges();

        public IActionResult Index()
        {
            return View("MyView", User());
        }

        public IActionResult ValidationSummary()
        {
            ModelState.AddModelError("something", "Something happened, show up in validation summary.");

            return View("ValidationSummary");
        }

        /// <summary>
        /// Action that shows metadata when model is <c>null</c>.
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Address = _addresses;
            ViewBag.Ages = _ages;

            return View();
        }

        /// <summary>
        /// Action that shows metadata when model is non-<c>null</c>.
        /// </summary>
        public IActionResult Edit(User user)
        {
            ViewBag.Address = _addresses;
            ViewBag.Age = _ages;
            ViewBag.Gift = "the banana";

            return View("Create");
        }

        /// <summary>
        /// Action that exercises query\form based model binding.
        /// </summary>
        public IActionResult SaveUser(User user)
        {
            return View("MyView", user);
        }

        /// <summary>
        /// Action that exercises input formatter
        /// </summary>
        public IActionResult Post([FromBody]User user)
        {
            return View("MyView", user);
        }

        public IActionResult Something()
        {
            return new ContentResult
            {
                Content = "Hello World From Content"
            };
        }

        public IActionResult Hello()
        {
            return Result.Content("Hello World");
        }

        public void Raw()
        {
            Context.Response.WriteAsync("Hello World raw");
        }

        public User User()
        {
            User user = new User()
            {
                Name = "My name",
                Address = "My address",
                Alive = true,
                Age = 13,
                GPA = 13.37M,
                Password = "Secure string",
                Dependent = new User()
                {
                    Name = "Dependents name",
                    Address = "Dependents address",
                    Alive = false,
                },
            };

            return user;
        }

        /// <summary>
        /// Action that exercises default view names.
        /// </summary>
        public IActionResult MyView()
        {
            return View(User());
        }

        private static IEnumerable<SelectListItem> CreateAddresses()
        {
            var addresses = new[]
            {
                "121 Fake St., Redmond, WA, USA",
                "123 Fake St., Redmond, WA, USA",
                "125 Fake St., Redmond, WA, USA",
                "127 Fake St., Redmond, WA, USA",
                "129 Fake St., Redmond, WA, USA",
                "131 Fake St., Redmond, WA, USA",
            };

            return new SelectList(addresses);
        }

        private static IEnumerable<SelectListItem> CreateAges()
        {
            var ages = Enumerable.Range(27, 47).Select(age => new { Age = age, Display = age.ToString("####"), });

            return new SelectList(ages, dataValueField: "Age", dataTextField: "Display");
        }
    }
}