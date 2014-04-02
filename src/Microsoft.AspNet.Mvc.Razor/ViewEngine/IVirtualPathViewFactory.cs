
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IVirtualPathViewFactory
    {
        IView CreateInstance(string virtualPath);
    }
}
