
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class OverloadController
    {
        private readonly IActionResultHelper _result;

        public OverloadController(IActionResultHelper result)
        {
            _result = result;
        }

        public IActionResult Get()
        {
            return _result.Content("Get()");
        }

        public IActionResult Get(int id)
        {
            return _result.Content("Get(id)");
        }

        public IActionResult Get(int id, string name)
        {
            return _result.Content("Get(id, name)");
        }
    }
}
