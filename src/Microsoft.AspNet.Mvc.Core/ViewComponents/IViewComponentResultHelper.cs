
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentResultHelper
    {
        IViewComponentResult Content([NotNull] string content);

        IViewComponentResult Json([NotNull] object value);

        IViewComponentResult View([NotNull] string viewName, [NotNull] ViewData viewData);
    }
}
