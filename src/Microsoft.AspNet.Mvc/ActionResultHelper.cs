using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultHelper : IActionResultHelper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewEngine _viewEngine;
        
        public ActionResultHelper(IServiceProvider serviceProvider, IViewEngine viewEngine)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
        }

        public IActionResult Content(string value)
        {
            return new ContentResult
            {
                Content = value
            };
        }

        public IActionResult Content(string value, string contentType)
        {
            return new ContentResult
            {
                Content = value,
                ContentType = contentType
            };
        }

        public IActionResult Json(object value)
        {
            return new JsonResult(value);
        }

        public IActionResult View(string view, ViewData viewData)
        {
            return new ViewResult(_serviceProvider, _viewEngine)
            {
                ViewName = view,
                ViewData = viewData
            };
        }
    }
}
