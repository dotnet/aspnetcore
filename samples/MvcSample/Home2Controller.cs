using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc;
using MvcSample.Models;

namespace MvcSample.RandomNameSpace
{
    public class Home2Controller
    {
        private User _user = new User() { Name = "User Name", Address = "Home Address" };

        public Home2Controller(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
        }

        public IActionResultHelper Result { get; private set; }

        public HttpContext Context { get; private set; }

        public string Index()
        {
            return "Hello World: my namespace is " + this.GetType().Namespace;
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
            Context.Response.WriteAsync("Hello World raw");
        }

        public IActionResult UserJson()
        {
            var jsonResult = Result.Json(_user);
            jsonResult.Indent = false;

            return jsonResult;
        }

        public User User()
        {
            return _user;
        }
    }
}