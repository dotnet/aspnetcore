using System;
using System.Text;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class Controller
    {
        private DynamicViewData _viewBag;

        public void Initialize(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
        }

        public HttpContext Context
        {
            get
            {
                return ActionContext.HttpContext;
            }
        }

        public ModelStateDictionary ModelState
        {
            get
            {
                return ViewData.ModelState;
            }
        }

        public ActionContext ActionContext { get; set; }

        public IActionResultHelper Result { get; private set; }

        public IUrlHelper Url { get; set; }

        public ViewDataDictionary<object> ViewData { get; set; }

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

        public IActionResult View()
        {
            return View(view: null);
        }

        public IActionResult View(string view)
        {
            return View(view, model: null);
        }

        // TODO #110: May need <TModel> here and in the overload below.
        public IActionResult View(object model)
        {
            return View(view: null, model: model);
        }

        public IActionResult View(string view, object model)
        {
            // Do not override ViewData.Model unless passed a non-null value.
            if (model != null)
            {
                ViewData.Model = model;
            }

            return Result.View(view, ViewData);
        }

        public IActionResult Content(string content)
        {
            return Content(content, contentType: null);
        }

        public IActionResult Content(string content, string contentType)
        {
            return Content(content, contentType, contentEncoding: null);
        }

        public IActionResult Content(string content, string contentType, Encoding contentEncoding)
        {
            return Result.Content(content, contentType, contentEncoding);
        }

        public IJsonResult Json(object value)
        {
            return Result.Json(value);
        }

        public virtual RedirectResult Redirect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            return new RedirectResult(url);
        }

        public virtual RedirectResult RedirectPermanent(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            return new RedirectResult(url, permanent: true);
        }
    }
}
