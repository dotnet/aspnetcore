using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class RenderPartialAsyncExtensions
    {
        
        /// <summary>
        /// Renders the partial view with the parent's view data and model.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the model.</typeparam>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync<T>(this IHtmlHelper<T> htmlHelper, [NotNull] string partialViewName)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model,
                                                 viewData: htmlHelper.ViewData);
        }

        /// <summary>
        /// Renders the partial view with the given view data and, implicitly, the given view data's model.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the model.</typeparam>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="viewData">
        /// The <see cref="ViewDataDictionary"/> that is provided to the partial view that will be rendered.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync<T>(this IHtmlHelper<T> htmlHelper, [NotNull] string partialViewName,
            ViewDataDictionary viewData)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, htmlHelper.ViewData.Model, viewData: viewData);
        }

        /// <summary>
        /// Renders the partial view with an empty view data and the given model.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the model.</typeparam>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <param name="partialViewName">The name of the partial view to render.</param>
        /// <param name="model">The model to provide to the partial view that will be rendered.</param>
        /// <returns>A <see cref="Task"/> that represents when rendering has completed.</returns>
        public static Task RenderPartialAsync<T>(this IHtmlHelper<T> htmlHelper, [NotNull] string partialViewName,
            object model)
        {
            return htmlHelper.RenderPartialAsync(partialViewName, model, htmlHelper.ViewData);
        }
    }
}
