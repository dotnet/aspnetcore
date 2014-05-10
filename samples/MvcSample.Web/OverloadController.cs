using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class OverloadController
    {
        private readonly IActionResultHelper _result;

        public OverloadController(IActionResultHelper result)
        {
            _result = result;
        }

        // All results implement IActionResult so it can be safely returned.
        public IActionResult Get()
        {
            return _result.Content("Get()", null, null);
        }

        public ActionResult Get(int id)
        {
            return _result.Content("Get(id)", null, null);
        }

        public ActionResult Get(int id, string name)
        {
            return _result.Content("Get(id, name)", null, null);
        }

        public ActionResult WithUser()
        {
            return _result.Content("WithUser()", null, null);
        }

        // Called for all posts regardless of values provided
        [HttpPost]
        public ActionResult WithUser(User user)
        {
            return _result.Content("WithUser(User)", null, null);
        }

        public ActionResult WithUser(int projectId, User user)
        {
            return _result.Content("WithUser(int, User)", null, null);
        }
    }
}
