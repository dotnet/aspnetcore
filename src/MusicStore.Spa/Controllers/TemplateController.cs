using System;
using Microsoft.AspNet.Mvc;

namespace MusicStore.Controllers
{
    public class TemplateController : Controller
    {
        private static readonly string _templateBasePath = "/ng-apps/";

        // GET: Template
        [Route("ng-apps/{*path}")]
        public ActionResult Index(string path)
        {
            if (!IsValidPath(path))
            {
                return new HttpNotFoundResult();
            }

            return View(_templateBasePath + path);
        }

        private static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var last = '\0';
            for (var i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (Char.IsLetterOrDigit(c)
                    || (c == '/' && last != '/')
                    || c == '-'
                    || c == '_'
                    || (c == '.' && last != '.'))
                {
                    last = c;
                    continue;
                }
                return false;
            }

            return path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
        }
    }
}