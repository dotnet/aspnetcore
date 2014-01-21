using System.Net.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Owin;

namespace MvcSample
{
    public class HomeController : Controller
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

        public IActionResult Hello()
        {
            return Result.Content("Hello World");
        }

        public void Raw()
        {
            Context.Response.Write("Hello World raw");
        }

        public HttpResponseMessage Hello2()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.Content = new StringContent("Hello World");

            return responseMessage;
        }

        public User User()
        {
            User user = new User()
            {
                Name = "My name",
                Address = "My address"
            };

            return user;
        }

        public IActionResult MyView()
        {
            ViewData.Model = User();
            return Result.View(ViewData);
        }
    }
}