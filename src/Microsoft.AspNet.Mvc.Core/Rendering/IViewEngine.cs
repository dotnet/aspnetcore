using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface IViewEngine
    {
        ViewEngineResult FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName);

        ViewEngineResult FindPartialView([NotNull] IDictionary<string, object> context, [NotNull] string partialViewName);
    }
}
