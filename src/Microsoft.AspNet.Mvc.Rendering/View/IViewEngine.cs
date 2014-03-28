using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface IViewEngine
    {
        Task<ViewEngineResult> FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName);
        Task<ViewEngineResult> FindPartialView([NotNull] IDictionary<string, object> context, [NotNull] string partialViewName);
    }
}
