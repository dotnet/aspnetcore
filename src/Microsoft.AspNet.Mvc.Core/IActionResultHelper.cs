using System.Text;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultHelper
    {
        ContentResult Content(string value, string contentType, Encoding contentEncoding);
        JsonResult Json(object value);
        ViewResult View(string view, ViewDataDictionary viewData);
    }
}
