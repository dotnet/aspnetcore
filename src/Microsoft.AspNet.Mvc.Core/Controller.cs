using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class Controller
    {
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

        public ViewData<object> ViewData { get; set; }

        public dynamic ViewBag
        {
            get { return ViewData; }
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
    }
}
