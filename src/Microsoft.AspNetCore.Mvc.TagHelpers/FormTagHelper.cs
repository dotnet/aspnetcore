// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;form&gt; elements.
    /// </summary>
    [HtmlTargetElement("form", Attributes = ActionAttributeName)]
    [HtmlTargetElement("form", Attributes = AntiforgeryAttributeName)]
    [HtmlTargetElement("form", Attributes = AreaAttributeName)]
    [HtmlTargetElement("form", Attributes = ControllerAttributeName)]
    [HtmlTargetElement("form", Attributes = RouteAttributeName)]
    [HtmlTargetElement("form", Attributes = RouteValuesDictionaryName)]
    [HtmlTargetElement("form", Attributes = RouteValuesPrefix + "*")]
    public class FormTagHelper : TagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string AntiforgeryAttributeName = "asp-antiforgery";
        private const string AreaAttributeName = "asp-area";
        private const string ControllerAttributeName = "asp-controller";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";
        private const string HtmlActionAttributeName = "action";
        private IDictionary<string, string> _routeValues;

        /// <summary>
        /// Creates a new <see cref="FormTagHelper"/>.
        /// </summary>
        /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
        public FormTagHelper(IHtmlGenerator generator)
        {
            Generator = generator;
        }

        // This TagHelper's order must be lower than the RenderAtEndOfFormTagHelper. I.e it must be executed before
        // RenderAtEndOfFormTagHelper does.
        /// <inheritdoc />
        public override int Order => -1000;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected IHtmlGenerator Generator { get; }

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
        /// Whether the antiforgery token should be generated.
        /// </summary>
        /// <value>Defaults to <c>false</c> if user provides an <c>action</c> attribute
        /// or if the <c>method</c> is <see cref="FormMethod.Get"/>; <c>true</c> otherwise.</value>
        [HtmlAttributeName(AntiforgeryAttributeName)]
        public bool? Antiforgery { get; set; }

        /// <summary>
        /// Name of the route.
        /// </summary>
        /// <remarks>
        /// Must be <c>null</c> if <see cref="Action"/> or <see cref="Controller"/> is non-<c>null</c>.
        /// </remarks>
        [HtmlAttributeName(RouteAttributeName)]
        public string Route { get; set; }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        /// <remarks>Passed through to the generated HTML in all cases.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Method { get; set; }

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
        /// <remarks>
        /// Does nothing if user provides an <c>action</c> attribute and <see cref="Antiforgery"/> is <c>null</c> or
        /// <c>false</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <c>action</c> attribute is provided and <see cref="Action"/> or <see cref="Controller"/> are
        /// non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
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
            if (Method != null)
            {
                output.CopyHtmlAttribute(nameof(Method), context);
            }

            var antiforgeryDefault = true;

            // If "action" is already set, it means the user is attempting to use a normal <form>.
            if (output.Attributes.ContainsName(HtmlActionAttributeName))
            {
                if (Action != null ||
                    Controller != null ||
                    Area != null ||
                    Route != null ||
                    (_routeValues != null && _routeValues.Count > 0))
                {
                    // User also specified bound attributes we cannot use.
                    throw new InvalidOperationException(
                        Resources.FormatFormTagHelper_CannotOverrideAction(
                            "<form>",
                            HtmlActionAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            AreaAttributeName,
                            RouteAttributeName,
                            RouteValuesPrefix));
                }

                // User is using the FormTagHelper like a normal <form> tag. Antiforgery default should be false to
                // not force the antiforgery token on the user.
                antiforgeryDefault = false;
            }
            else
            {
                IDictionary<string, object> routeValues = null;
                if (_routeValues != null && _routeValues.Count > 0)
                {
                    // Convert from Dictionary<string, string> to Dictionary<string, object>.
                    routeValues = new Dictionary<string, object>(_routeValues.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var routeValue in _routeValues)
                    {
                        routeValues.Add(routeValue.Key, routeValue.Value);
                    }
                }

                if (Area != null)
                {
                    if (routeValues == null)
                    {
                        routeValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    }

                    // Unconditionally replace any value from asp-route-area.
                    routeValues["area"] = Area;
                }

                TagBuilder tagBuilder;
                if (Route == null)
                {
                    tagBuilder = Generator.GenerateForm(
                        ViewContext,
                        Action,
                        Controller,
                        routeValues,
                        method: null,
                        htmlAttributes: null);
                }
                else if (Action != null || Controller != null)
                {
                    // Route and Action or Controller were specified. Can't determine the action attribute.
                    throw new InvalidOperationException(
                        Resources.FormatFormTagHelper_CannotDetermineActionWithRouteAndActionOrControllerSpecified(
                            "<form>",
                            RouteAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            HtmlActionAttributeName));
                }
                else
                {
                    tagBuilder = Generator.GenerateRouteForm(
                        ViewContext,
                        Route,
                        routeValues,
                        method: null,
                        htmlAttributes: null);
                }

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);
                    if (tagBuilder.HasInnerHtml)
                    {
                        output.PostContent.AppendHtml(tagBuilder.InnerHtml);
                    }
                }

                if (string.Equals(Method, "get", StringComparison.OrdinalIgnoreCase))
                {
                    antiforgeryDefault = false;
                }
            }

            if (Antiforgery ?? antiforgeryDefault)
            {
                var antiforgeryTag = Generator.GenerateAntiforgery(ViewContext);
                if (antiforgeryTag != null)
                {
                    output.PostContent.AppendHtml(antiforgeryTag);
                }
            }
        }
    }
}