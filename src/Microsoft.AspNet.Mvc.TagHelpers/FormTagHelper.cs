// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;form&gt; elements.
    /// </summary>
    [TargetElement("form", Attributes = ActionAttributeName)]
    [TargetElement("form", Attributes = AntiForgeryAttributeName)]
    [TargetElement("form", Attributes = ControllerAttributeName)]
    [TargetElement("form", Attributes = RouteAttributeName)]
    [TargetElement("form", Attributes = RouteValuesDictionaryName)]
    [TargetElement("form", Attributes = RouteValuesPrefix + "*")]
    public class FormTagHelper : TagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string AntiForgeryAttributeName = "asp-anti-forgery";
        private const string ControllerAttributeName = "asp-controller";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";
        private const string HtmlActionAttributeName = "action";

        /// <summary>
        /// Creates a new <see cref="FormTagHelper"/>.
        /// </summary>
        /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
        public FormTagHelper(IHtmlGenerator generator)
        {
            Generator = generator;
        }

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
        /// Whether the anti-forgery token should be generated.
        /// </summary>
        /// <value>Defaults to <c>false</c> if user provides an <c>action</c> attribute; <c>true</c> otherwise.</value>
        [HtmlAttributeName(AntiForgeryAttributeName)]
        public bool? AntiForgery { get; set; }

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
        public IDictionary<string, string> RouteValues { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        /// <remarks>
        /// Does nothing if user provides an <c>action</c> attribute and <see cref="AntiForgery"/> is <c>null</c> or
        /// <c>false</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <c>action</c> attribute is provided and <see cref="Action"/> or <see cref="Controller"/> are
        /// non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var antiForgeryDefault = true;

            // If "action" is already set, it means the user is attempting to use a normal <form>.
            if (output.Attributes.ContainsName(HtmlActionAttributeName))
            {
                if (Action != null || Controller != null || Route != null || RouteValues.Count != 0)
                {
                    // User also specified bound attributes we cannot use.
                    throw new InvalidOperationException(
                        Resources.FormatFormTagHelper_CannotOverrideAction(
                            "<form>",
                            HtmlActionAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            RouteAttributeName,
                            RouteValuesPrefix));
                }

                // User is using the FormTagHelper like a normal <form> tag. Anti-forgery default should be false to
                // not force the anti-forgery token on the user.
                antiForgeryDefault = false;
            }
            else
            {
                // Convert from Dictionary<string, string> to Dictionary<string, object>.
                var routeValues = RouteValues.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value,
                    StringComparer.OrdinalIgnoreCase);

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
                    output.PostContent.Append(tagBuilder.InnerHtml);
                }
            }

            if (AntiForgery ?? antiForgeryDefault)
            {
                var antiForgeryTagBuilder = Generator.GenerateAntiForgery(ViewContext);
                if (antiForgeryTagBuilder != null)
                {
                    output.PostContent.Append(antiForgeryTagBuilder.ToString(TagRenderMode.SelfClosing));
                }
            }
        }
    }
}