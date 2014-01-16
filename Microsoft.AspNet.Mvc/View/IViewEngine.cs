using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewEngine
    {
        Task<ViewEngineResult> FindView(RequestContext requestContext, string viewName);
    }
}
