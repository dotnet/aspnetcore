
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IVirtualPathFactory
    {
        Task<object> CreateInstance(string virtualPath);
    }
}
