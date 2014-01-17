
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IVirtualPathViewFactory
    {
        Task<IView> CreateInstance(string virtualPath);
    }
}
