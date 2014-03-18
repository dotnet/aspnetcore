using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelper<TModel> : HtmlHelper
    {
        public HtmlHelper([NotNull]HttpContext httpContext, ViewData<TModel> viewData)
            : base(httpContext, viewData)
        {
            ViewData = viewData;
        }

        public new ViewData<TModel> ViewData
        {
            get; private set;
        }
    }
}
