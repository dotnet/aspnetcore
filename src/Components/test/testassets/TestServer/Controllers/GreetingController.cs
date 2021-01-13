using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers
{
    [Route("api/[controller]/[action]")]
    public class GreetingController : Controller
    {
        [HttpGet]
        public string SayHello() => "Hello";
    }
}
