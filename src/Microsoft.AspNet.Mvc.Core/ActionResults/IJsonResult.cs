using System.Text;

namespace Microsoft.AspNet.Mvc
{
    public interface IJsonResult : IActionResult
    {
        Encoding Encoding { get; set; }
        bool Indent { get; set; }
    }
}
