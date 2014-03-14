using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class SimpleRest : Controller
    {
        public string Get()
        {
            return "Get method";
        }
    }
}
