
namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Name-related extensions for <see cref="HtmlHelper"/> and <see cref="HtmlHelper{T}"/>.
    /// </summary>
    public static class HtmlHelperNameExtensions
    {
        /// <summary>
        /// Gets the full HTML field name for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="HtmlHelper"/> instance that this method extends.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        public static HtmlString NameForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return htmlHelper.Name(string.Empty);
        }
    }
}
