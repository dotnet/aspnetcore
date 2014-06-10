using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    [Route("api/REST")]
    public class SimpleRest : Controller
    {
        [HttpGet]
        public string ThisIsAGetMethod()
        {
            return "Get method";
        }

        [HttpGet("OtherThing")]
        public string GetOtherThing()
        {
            return "Get other thing";
        }
    }
}
