using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class OverloadController
    {
        // All results implement IActionResult so it can be safely returned.
        public IActionResult Get()
        {
            return Content("Get()");
        }

        public ActionResult Get(int id)
        {
            return Content("Get(id)");
        }

        public ActionResult Get(int id, string name)
        {
            return Content("Get(id, name)");
        }

        public ActionResult WithUser()
        {
            return Content("WithUser()");
        }

        // Called for all posts regardless of values provided
        [HttpPost]
        public ActionResult WithUser(User user)
        {
            return Content("WithUser(User)");
        }

        public ActionResult WithUser(int projectId, User user)
        {
            return Content("WithUser(int, User)");
        }

        private ContentResult Content(string content)
        {
            var result = new ContentResult
            {
                Content = content,
            };

            return result;
        }
    }
}
