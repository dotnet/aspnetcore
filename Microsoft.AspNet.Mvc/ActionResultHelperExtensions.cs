
namespace Microsoft.AspNet.Mvc
{
    public static class ActionResultHelperExtensions
    {
        public static IActionResult View(this IActionResultHelper actionResultHelper)
        {
            return actionResultHelper.View(view: null, model: null);
        }

        public static IActionResult View(this IActionResultHelper actionResultHelper, string view)
        {
            return actionResultHelper.View(view, model: null);
        }

        public static IActionResult View(this IActionResultHelper actionResultHelper, object model)
        {
            return actionResultHelper.View(view: null, model: model);
        }
    }
}
