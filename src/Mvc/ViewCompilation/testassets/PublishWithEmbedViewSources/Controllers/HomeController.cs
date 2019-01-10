using System;
using System.IO;
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
            var precompiledAssemblyPath = Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location),
                "PublishWithEmbedViewSources.PrecompiledViews.dll");
            var precompiledAssembly = Assembly.LoadFile(precompiledAssemblyPath);
            return string.Join(
                Environment.NewLine,
                precompiledAssembly.GetManifestResourceNames());
        }
    }
}
