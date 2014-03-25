using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultHelper
    {
        IActionResult Content(string value);
        IActionResult Content(string value, string contentType);
        IJsonResult Json(object value);
        IActionResult View(string view, ViewDataDictionary viewData);
    }
}
