using System.Text;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultHelper
    {
        IActionResult Content(string value);
        IActionResult Content(string value, string contentType);
        IActionResult Content(string value, string contentType, Encoding contentEncoding);
        IJsonResult Json(object value);
        IActionResult View(string view, ViewDataDictionary viewData);
    }
}
