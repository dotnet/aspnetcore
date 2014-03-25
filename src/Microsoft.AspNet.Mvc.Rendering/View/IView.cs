using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface IView
    {
        Task RenderAsync([NotNull] ViewContext context);
    }
}
