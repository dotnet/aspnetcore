using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IView
    {
        Task RenderAsync([NotNull] ViewContext context);
    }
}
