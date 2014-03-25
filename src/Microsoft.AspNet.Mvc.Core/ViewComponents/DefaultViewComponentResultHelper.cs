

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentResultHelper : IViewComponentResultHelper
    {
        private readonly IViewEngine _viewEngine;

        public DefaultViewComponentResultHelper(IViewEngine viewEngine)
        {
            _viewEngine = viewEngine;
        }

        public IViewComponentResult Content([NotNull] string content)
        {
            return new ContentViewComponentResult(content);
        }

        public IViewComponentResult Json([NotNull] object value)
        {
            return new JsonViewComponentResult(value);
        }

        public IViewComponentResult View([NotNull] string viewName, [NotNull] ViewDataDictionary viewData)
        {
            return new ViewViewComponentResult(_viewEngine, viewName, viewData);
        }
    }
}
