using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewEngine
    {
        ViewEngineResult FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName);

        ViewEngineResult FindPartialView([NotNull] IDictionary<string, object> context, [NotNull] string partialViewName);
    }
}
