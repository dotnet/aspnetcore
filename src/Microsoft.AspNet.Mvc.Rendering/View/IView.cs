using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public interface IView
    {
        Task RenderAsync(ViewContext context, TextWriter writer);
    }
}
