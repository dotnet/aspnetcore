using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace PublishWithEmbedViewSources.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public string GetPrecompiledResourceNames()
        {
            var precompiledAssembly = Assembly.Load(
                new AssemblyName("PublishWithEmbedViewSources.PrecompiledViews"));
            return string.Join(
                Environment.NewLine,
                precompiledAssembly.GetManifestResourceNames());
        }
    }
}
