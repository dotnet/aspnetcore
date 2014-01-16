using System;
using Microsoft.AspNet.CoreServices;

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
            throw new NotImplementedException();
        }

        public IActionResult View()
        {
            return new ViewResult(_serviceProvider, _viewEngine)
            {
                ViewName = null,
                Model = null
            };
        }
    }
}
