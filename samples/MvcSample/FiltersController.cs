using Microsoft.AspNet.Mvc;
using MvcSample.Filters;
using MvcSample.Models;

namespace MvcSample
{
    // Expected order in descriptor - object -> int -> string
    // TODO: Add a real filter here
    [ServiceFilter(typeof(PassThroughAttribute), Order = 1)]
    [ServiceFilter(typeof(PassThroughAttribute))]
    [PassThrough(Order = 0)]
    [PassThrough(Order = 2)]
    public class FiltersController : Controller
    {
        private readonly User _user = new User() { Name = "User Name", Address = "Home Address" };

        // TODO: Add a real filter here
        [ServiceFilter(typeof(PassThroughAttribute))]
        [AgeEnhancer]
        [InspectResultPage]
        public IActionResult Index(int age)
        {
            _user.Age = age;

            return View("MyView", _user);
        }
    }   
}