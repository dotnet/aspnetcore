using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class HomeController : Controller
    {
        private static readonly IEnumerable<SelectListItem> _addresses = CreateAddresses();
        private static readonly IEnumerable<SelectListItem> _ages = CreateAges();

        public ActionResult Index()
        {
            return View("MyView", CreateUser());
        }

        public IActionResult NullUser()
        {
            return View();
        }

        public ActionResult ValidationSummary()
        {
            ModelState.AddModelError("something", "Something happened, show up in validation summary.");

            return View("ValidationSummary");
        }

        public ActionResult InjectSample()
        {
            return View();
        }

        public ActionResult NotFound()
        {
            return HttpNotFound();
        }

        public ActionResult SendFileFromDisk()
        {
            return File("sample.txt", "text/plain");
        }

        public ActionResult SendFileFromDiskWithName()
        {
            return File("sample.txt", "text/plain", "sample-file.txt");
        }

        public bool IsDefaultNameSpace()
        {
            var namespaceToken = ActionContext.RouteData.DataTokens["NameSpace"] as string;
            return namespaceToken == "default";
        }

        /// <summary>
        /// Action that shows metadata when model is <c>null</c>.
        /// </summary>
        public ActionResult Create()
        {
            ViewBag.Address = _addresses;
            ViewBag.Ages = _ages;

            return View();
        }

        /// <summary>
        /// Action that shows metadata when model is non-<c>null</c>.
        /// </summary>
        public ActionResult Edit(User user)
        {
            ViewBag.Address = _addresses;
            ViewBag.Ages = _ages;
            ViewBag.Gift = "the banana";

            return View("Create");
        }

        /// <summary>
        /// Action that exercises query\form based model binding.
        /// </summary>
        public ActionResult SaveUser(User user)
        {
            return View("MyView", user);
        }

        /// <summary>
        /// Action that exercises input formatter
        /// </summary>
        public ActionResult Post([FromBody]User user)
        {
            return View("MyView", user);
        }

        public ActionResult Something()
        {
            return new ContentResult
            {
                Content = "Hello World From Content"
            };
        }

        public ActionResult Hello()
        {
            return Content("Hello World");
        }

        public void Raw()
        {
            Context.Response.WriteAsync("Hello World raw");
        }

        public ActionResult Language()
        {
            return View();
        }

        [Produces("application/json", "application/xml", "application/custom", "text/json", Type = typeof(User))]
        public object ReturnUser()
        {
            return CreateUser();
        }

        public User CreateUser()
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
                Profession = "Software Engineer",
                About = "I like playing Football"
            };

            return user;
        }

        [HttpGet("/AttributeRouting/{other}", Order = 0)]
        public string LowerPrecedence(string param)
        {
            return "Lower";
        }

        // Normally this route would be tried before the one above
        // as it is more explicit (doesn't have a parameter), but
        // due to the fact that it has a higher order, it will be
        // tried after the route above.
        [HttpGet("/AttributeRouting/HigherPrecedence", Order = 1)]
        public string HigherOrder()
        {
            return "Higher";
        }

        // Both routes have the same template, which would make
        // them ambiguous, but the order we defined in the routes
        // disambiguates them.
        [HttpGet("/AttributeRouting/SameTemplate", Order = 0)]
        public string SameTemplateHigherOrderPrecedence()
        {
            return "HigherOrderPrecedence";
        }

        [HttpGet("/AttributeRouting/SameTemplate", Order = 1)]
        public string SameTemplateLowerOrderPrecedence()
        {
            return "LowerOrderPrecedence";
        }

        /// <summary>
        /// Action that exercises default view names.
        /// </summary>
        public ActionResult MyView()
        {
            return View(CreateUser());
        }

        public ActionResult FlushPoint()
        {
            return View();
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