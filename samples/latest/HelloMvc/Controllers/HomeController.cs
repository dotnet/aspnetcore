using Microsoft.AspNetCore.Mvc;

namespace HelloMvc
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(CreateUser());
        }

        public User CreateUser()
        {
            User user = new User()
            {
                Name = "My name",
                Address = "My address"
            };

            return user;
        }
    }
}