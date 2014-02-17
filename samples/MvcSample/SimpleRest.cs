using Microsoft.AspNet.Mvc;

namespace MvcSample
{
    public class SimpleRest : Controller
    {
        public string Get()
        {
            return "Get method";
        }
    }
}
