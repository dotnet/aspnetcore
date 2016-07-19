// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;button&gt; elements.
    /// </summary>
    [HtmlTargetElement("button", Attributes = ActionAttributeName)]
    [HtmlTargetElement("button", Attributes = ControllerAttributeName)]
    [HtmlTargetElement("button", Attributes = AreaAttributeName)]
    [HtmlTargetElement("button", Attributes = RouteAttributeName)]
    [HtmlTargetElement("button", Attributes = RouteValuesDictionaryName)]
    [HtmlTargetElement("button", Attributes = RouteValuesPrefix + "*")]
    public class ButtonTagHelper : TagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string ControllerAttributeName = "asp-controller";
        private const string AreaAttributeName = "asp-area";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";
        private const string FormAction = "formaction";
        private IDictionary<string, string> _routeValues;

        /// <summary>
        /// Creates a new <see cref="ButtonTagHelper"/>.
        /// </summary>
        /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
        public ButtonTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            UrlHelperFactory = urlHelperFactory;
        }

        /// <inheritdoc />
        public override int Order => -1000;

        /// <summary>
        /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected IUrlHelperFactory UrlHelperFactory { get; }

        /// <summary>
        /// The name of the action method.
        /// </summary>
        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        /// <summary>
        /// The name of the controller.
        /// </summary>
        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        /// <summary>
        /// The name of the area.
        /// </summary>
        [HtmlAttributeName(AreaAttributeName)]
        public string Area { get; set; }

        /// <summary>
        /// Name of the route.
        /// </summary>
        /// <remarks>
        /// Must be <c>null</c> if <see cref="Action"/> or <see cref="Controller"/> is non-<c>null</c>.
        /// </remarks>
        [HtmlAttributeName(RouteAttributeName)]
        public string Route { get; set; }

        /// <summary>
        /// Additional parameters for the route.
        /// </summary>
        [HtmlAttributeName(RouteValuesDictionaryName, DictionaryAttributePrefix = RouteValuesPrefix)]
        public IDictionary<string, string> RouteValues
        {
            get
            {
                if (_routeValues == null)
                {
                    _routeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                return _routeValues;
            }
            set
            {
                _routeValues = value;
            }
        }

        /// <inheritdoc />
        /// <remarks>Does nothing if user provides an <c>formaction</c> attribute.</remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <c>formaction</c> attribute is provided and <see cref="Action"/>, <see cref="Controller"/>,
        /// or <see cref="Route"/> are non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
        /// Also thrown if <see cref="Route"/> and one or both of <see cref="Action"/> and <see cref="Controller"/>
        /// are non-<c>null</c>
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            // If "formaction" is already set, it means the user is attempting to use a normal button.
            if (output.Attributes.ContainsName(FormAction))
            {
                if (Action != null || Controller != null || Area != null || Route != null || RouteValues.Count != 0)
                {
                    // User specified a formaction and one of the bound attributes; can't determine the formaction attribute.
                    throw new InvalidOperationException(
                        Resources.FormatButtonTagHelper_CannotOverrideFormAction(
                            "<button>",
                            ActionAttributeName,
                            ControllerAttributeName,
                            AreaAttributeName,
                            RouteAttributeName,
                            RouteValuesPrefix,
                            FormAction));
                }
            }
            else
            {
                RouteValueDictionary routeValues = null;
                if (_routeValues != null && _routeValues.Count > 0)
                {
                    routeValues = new RouteValueDictionary(_routeValues);
                }

                if (Area != null)
                {
                    if (routeValues == null)
                    {
                        routeValues = new RouteValueDictionary();
                    }

                    // Unconditionally replace any value from asp-route-area. 
                    routeValues["area"] = Area;
                }

                if (Route == null)
                {
                    var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
                    var url = urlHelper.Action(Action, Controller, routeValues);
                    output.Attributes.SetAttribute(FormAction, url);
                }
                else if (Action != null || Controller != null)
                {
                    // Route and Action or Controller were specified. Can't determine the formaction attribute.
                    throw new InvalidOperationException(
                        Resources.FormatButtonTagHelper_CannotDetermineFormActionRouteActionOrControllerSpecified(
                            "<button>",
                            RouteAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            FormAction));
                }
                else
                {
                    var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
                    var url = urlHelper.RouteUrl(Route, routeValues);
                    output.Attributes.SetAttribute(FormAction, url);
                }
            }
        }
    }
}