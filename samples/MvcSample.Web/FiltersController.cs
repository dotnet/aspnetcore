using System;
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Filters;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    [ServiceFilter(typeof(PassThroughAttribute), Order = 1)]
    [ServiceFilter(typeof(PassThroughAttribute))]
    [PassThrough(Order = 0)]
    [PassThrough(Order = 2)]
    [InspectResultPage]
    [BlockAnonymous]
    [UserNameProvider(Order = -1)]
    public class FiltersController : Controller
    {
        private readonly User _user = new User() { Name = "User Name", Address = "Home Address" };

        // TODO: Add a real filter here
        [ServiceFilter(typeof(PassThroughAttribute))]
        [AllowAnonymous]
        [AgeEnhancer]
        [Delay(500)]
        public IActionResult Index(int age, string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                _user.Name = userName;
            }

            _user.Age = age;

            return View("MyView", _user);
        }

        public IActionResult Blocked(int age, string userName)
        {
            return Index(age, userName);
        }

        [ErrorMessages, AllowAnonymous]
        public IActionResult Crash(string message)
        {
            throw new Exception(message);
        }
    }
}