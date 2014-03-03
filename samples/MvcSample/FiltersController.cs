using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MvcSample.Models;

namespace MvcSample
{
    // Expected order in descriptor - object -> int -> string
    // TODO: Add a real filter here
    [ServiceFilter(typeof(object), Order = 1)]
    [ServiceFilter(typeof(string))]
    [PassThrough(Order = 0)]
    [PassThrough(Order = 2)]
    public class FiltersController : Controller
    {
        private readonly User _user = new User() { Name = "User Name", Address = "Home Address" };

        // TODO: Add a real filter here
        [ServiceFilter(typeof(int))]
        public IActionResult Index()
        {
            return View("MyView", _user);
        }
    }

    public class PassThroughAttribute : AuthorizationFilterAttribute
    {
        public async override Task Invoke(AuthorizationFilterContext context, Func<AuthorizationFilterContext, Task> next)
        {
            await next(context);
        }
    }
}