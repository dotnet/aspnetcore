using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var identity = new ClaimsIdentity("playerLogin");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "sdfsdfd"));
            identity.AddClaim(new Claim(ClaimTypes.Name, "sdfsdfd"));
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("playerLogin", principal);

            return View();
        }

        [Authorize(AuthenticationSchemes = "playerLogin")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
