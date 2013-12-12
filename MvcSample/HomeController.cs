using Microsoft.AspNet.Mvc;

namespace MvcSample
{
    public class HomeController
    {
        public string Index()
        {
            return "Hello World";
        }

        public IActionResult Something()
        {
            return new ContentResult
            {
                Content = "Hello World From Content"
            };
        }
    }
}