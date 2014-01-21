
namespace Microsoft.AspNet.Mvc
{
    public static class ActionResultHelperExtensions
    {
        public static IActionResult View(this IActionResultHelper actionResultHelper)
        {
            return actionResultHelper.View(view: null, viewData: null);
        }

        public static IActionResult View(this IActionResultHelper actionResultHelper, string view)
        {
            return actionResultHelper.View(view, viewData: null);
        }

        public static IActionResult View(this IActionResultHelper actionResultHelper, ViewDataDictionary viewData)
        {
            return actionResultHelper.View(view: null, viewData: viewData);
        }
    }
}
