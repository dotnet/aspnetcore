
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class ViewComponentHelperExtensions
    {
        public static HtmlString Invoke<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            return helper.Invoke(typeof(TComponent), args);
        }

        public static void RenderInvoke<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            helper.RenderInvoke(typeof(TComponent), args);
        }

        public static async Task<HtmlString> InvokeAsync<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            return await helper.InvokeAsync(typeof(TComponent), args);
        }

        public static async Task RenderInvokeAsync<TComponent>([NotNull] this IViewComponentHelper helper,
            params object[] args)
        {
            await helper.RenderInvokeAsync(typeof(TComponent), args);
        }
    }
}
