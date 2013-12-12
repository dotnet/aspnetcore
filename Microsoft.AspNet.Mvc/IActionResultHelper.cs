
namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultHelper
    {
        IActionResult Content(string value);
        IActionResult Content(string value, string contentType);
        IActionResult Json(object value);
    }
}
