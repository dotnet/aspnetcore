using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultHelper : IActionResultHelper
    {
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
            throw new NotImplementedException();
        }
    }
}
