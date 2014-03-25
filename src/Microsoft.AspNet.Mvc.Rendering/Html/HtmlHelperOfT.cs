using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelper<TModel> : HtmlHelper
    {
        public HtmlHelper([NotNull]HttpContext httpContext, ViewDataDictionary<TModel> viewData)
            : base(httpContext, viewData)
        {
            ViewData = viewData;
        }

        public new ViewDataDictionary<TModel> ViewData
        {
            get; private set;
        }
    }
}
