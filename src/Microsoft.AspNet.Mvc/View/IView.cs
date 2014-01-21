using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IView
    {
        Task RenderAsync(ViewContext context, TextWriter writer);
    }
}
