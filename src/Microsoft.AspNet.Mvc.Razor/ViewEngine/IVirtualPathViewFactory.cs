
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IVirtualPathViewFactory
    {
        Task<IView> CreateInstance(string virtualPath);
    }
}
