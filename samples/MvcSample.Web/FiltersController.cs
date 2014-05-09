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
    [TypeFilter(typeof(UserNameProvider), Order = -1)]
    public class FiltersController : Controller
    {
        public User CustomUser { get; set; }

        public FiltersController()
        {
            CustomUser = new User() { Name = "User Name", Address = "Home Address" };
        }

        // TODO: Add a real filter here
        [ServiceFilter(typeof(PassThroughAttribute))]
        [AllowAnonymous]
        [AgeEnhancer]
        [Delay(500)]
        public ActionResult Index(int age, string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                CustomUser.Name = userName;
            }

            CustomUser.Age = age;

            return View("MyView", CustomUser);
        }

        public ActionResult Blocked(int age, string userName)
        {
            return Index(age, userName);
        }

        [Authorize("Permission", "CanViewPage")]
        public ActionResult NotGrantedClaim(int age, string userName)
        {
            return Index(age, userName);
        }

        [FakeUser]
        [Authorize("Permission", "CanViewPage", "CanViewAnything")]
        public ActionResult AllGranted(int age, string userName)
        {
            return Index(age, userName);
        }

        [ErrorMessages, AllowAnonymous]
        public ActionResult Crash(string message)
        {
            throw new Exception(message);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.DidTheFilterRun = "Totally!";
        }
    }
}