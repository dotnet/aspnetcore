using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;

namespace MvcSample.Web
{
    /// <summary>
    /// A <see cref="IViewLocationExpander"/> that replaces adds the language as an extension prefix to view names.
    /// </summary>
    /// <example>
    /// For the default case with no areas, views are generated with the following patterns (assuming controller is
    /// "Home", action is "Index" and language is "en")
    /// Views/Home/en/Action
    /// Views/Home/Action
    /// Views/Shared/en/Action
    /// Views/Shared/Action
    /// </example>
    public class LanguageViewLocationExpander : IViewLocationExpander
    {
        private const string ValueKey = "language";
        private readonly Func<ActionContext, string> _valueFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="LanguageViewLocationExpander"/>.
        /// </summary>
        /// <param name="valueFactory">A factory that provides tbe language to use for expansion.</param>
        public LanguageViewLocationExpander(Func<ActionContext, string> valueFactory)
        {
            _valueFactory = valueFactory;
        }

        /// <inheritdoc />
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var value = _valueFactory(context.ActionContext);
            if (!string.IsNullOrEmpty(value))
            {
                context.Values[ValueKey] = value;
            }
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
                                                               IEnumerable<string> viewLocations)
        {
            string value;
            if (context.Values.TryGetValue(ValueKey, out value))
            {
                return ExpandViewLocationsCore(viewLocations, value);
            }

            return viewLocations;
        }

        private IEnumerable<string> ExpandViewLocationsCore(IEnumerable<string> viewLocations,
                                                            string value)
        {
            foreach (var location in viewLocations)
            {
                yield return location.Replace("{0}", value + "/{0}");
                yield return location;
            }
        }
    }
}