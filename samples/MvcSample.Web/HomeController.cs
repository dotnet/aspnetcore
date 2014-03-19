using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("MyView", User());
        }

        /// <summary>
        /// Action that shows metadata when model is <c>null</c>.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Action that shows metadata when model is non-<c>null</c>.
        /// </summary>
        /// <returns></returns>
        public IActionResult Edit()
        {
            ViewBag.Gift = "the banana";
            ViewData.Model = new User { Name = "Name", Address = "Address in a State", Age = 37, };
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
                Address = "My address"
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
    }
}