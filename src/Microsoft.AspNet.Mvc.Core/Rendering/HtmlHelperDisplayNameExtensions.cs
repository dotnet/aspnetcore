
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// DisplayName-related extensions for <see cref="HtmlHelper"/> and <see cref="IHtmlHelper{T}"/>.
    /// </summary>
    public static class HtmlHelperDisplayNameExtensions
    {
        /// <summary>
        /// Gets the display name for the current model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{T}"/> instance that this method extends.</param>
        /// <returns>An <see cref="HtmlString"/> that represents HTML markup.</returns>
        public static HtmlString DisplayNameForModel<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper)
        {
            return htmlHelper.DisplayName(string.Empty);
        }

        /// <summary>
        /// Gets the display name for the model.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper{T}"/> instance that this method extends.</param>
        /// <param name="expression">An expression that identifies the object that contains the display name.</param>
        /// <returns>
        /// The display name for the model.
        /// </returns>
        public static HtmlString DisplayNameFor<TInnerModel,TValue>(this IHtmlHelper<IEnumerable<TInnerModel>> htmlHelper,
                                                                      Expression<Func<TInnerModel, TValue>> expression)
        {
            return htmlHelper.DisplayNameForInnerType<TInnerModel, TValue>(expression);
        }
    }
}
