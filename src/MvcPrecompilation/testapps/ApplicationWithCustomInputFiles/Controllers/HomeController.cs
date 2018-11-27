using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace ApplicationWithCustomInputFiles.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public string GetPrecompiledResourceNames([FromServices] ApplicationPartManager applicationManager)
        {
            var feature = new ViewsFeature();
            applicationManager.PopulateFeature(feature);
            return string.Join(Environment.NewLine, feature.ViewDescriptors.Select(v => v.RelativePath));
        }
    }
}
