
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    [ViewComponent]
    public abstract class ViewComponent
    {
        private dynamic _viewBag;

        public HttpContext Context
        {
            get { return ViewContext == null ? null : ViewContext.HttpContext; }
        }

        public IViewComponentResultHelper Result { get; private set; }

        public dynamic ViewBag
        {
            get
            {
                if (_viewBag == null)
                {
                    _viewBag = new DynamicViewData(() => ViewData);
                }

                return _viewBag;
            }
        }

        public ViewContext ViewContext { get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public void Initialize(IViewComponentResultHelper result)
        {
            Result = result;
        }

        public IViewComponentResult View()
        {
            return View<object>(null, null);
        }

        public IViewComponentResult View(string viewName)
        {
            return View<object>(viewName, null);
        }

        public IViewComponentResult View<TModel>(TModel model)
        {
            return View(null, model);
        }

        public IViewComponentResult View<TModel>(string viewName, TModel model)
        {
            var viewData = new ViewDataDictionary<TModel>(ViewData);
            if (model != null)
            {
                viewData.Model = model;
            }

            return Result.View(viewName ?? "Default", viewData);
        }
    }
}
