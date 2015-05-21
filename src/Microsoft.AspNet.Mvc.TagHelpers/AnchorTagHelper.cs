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
    /// <see cref="ITagHelper"/> implementation targeting &lt;a&gt; elements.
    /// </summary>
    [TargetElement("a", Attributes = ActionAttributeName)]
    [TargetElement("a", Attributes = ControllerAttributeName)]
    [TargetElement("a", Attributes = FragmentAttributeName)]
    [TargetElement("a", Attributes = HostAttributeName)]
    [TargetElement("a", Attributes = ProtocolAttributeName)]
    [TargetElement("a", Attributes = RouteAttributeName)]
    [TargetElement("a", Attributes = RouteValuesDictionaryName)]
    [TargetElement("a", Attributes = RouteValuesPrefix + "*")]
    public class AnchorTagHelper : TagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string ControllerAttributeName = "asp-controller";
        private const string FragmentAttributeName = "asp-fragment";
        private const string HostAttributeName = "asp-host";
        private const string ProtocolAttributeName = "asp-protocol";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";
        private const string Href = "href";

        /// <summary>
        /// Creates a new <see cref="AnchorTagHelper"/>.
        /// </summary>
        /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
        public AnchorTagHelper(IHtmlGenerator generator)
        {
            Generator = generator;
        }

        protected IHtmlGenerator Generator { get; }

        /// <summary>
        /// The name of the action method.
        /// </summary>
        /// <remarks>Must be <c>null</c> if <see cref="Route"/> is non-<c>null</c>.</remarks>
        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        /// <summary>
        /// The name of the controller.
        /// </summary>
        /// <remarks>Must be <c>null</c> if <see cref="Route"/> is non-<c>null</c>.</remarks>
        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        /// <summary>
        /// The protocol for the URL, such as &quot;http&quot; or &quot;https&quot;.
        /// </summary>
        [HtmlAttributeName(ProtocolAttributeName)]
        public string Protocol { get; set; }

        /// <summary>
        /// The host name.
        /// </summary>
        [HtmlAttributeName(HostAttributeName)]
        public string Host { get; set; }

        /// <summary>
        /// The URL fragment name.
        /// </summary>
        [HtmlAttributeName(FragmentAttributeName)]
        public string Fragment { get; set; }

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
        /// <remarks>Does nothing if user provides an <c>href</c> attribute.</remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <c>href</c> attribute is provided and <see cref="Action"/>, <see cref="Controller"/>,
        /// <see cref="Fragment"/>, <see cref="Host"/>, <see cref="Protocol"/>, or <see cref="Route"/> are
        /// non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes. Also thrown if <see cref="Route"/>
        /// and one or both of <see cref="Action"/> and <see cref="Controller"/> are non-<c>null</c>.
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // If "href" is already set, it means the user is attempting to use a normal anchor.
            if (output.Attributes.ContainsName(Href))
            {
                if (Action != null ||
                    Controller != null ||
                    Route != null ||
                    Protocol != null ||
                    Host != null ||
                    Fragment != null ||
                    RouteValues.Count != 0)
                {
                    // User specified an href and one of the bound attributes; can't determine the href attribute.
                    throw new InvalidOperationException(
                        Resources.FormatAnchorTagHelper_CannotOverrideHref(
                            "<a>",
                            ActionAttributeName,
                            ControllerAttributeName,
                            RouteAttributeName,
                            ProtocolAttributeName,
                            HostAttributeName,
                            FragmentAttributeName,
                            RouteValuesPrefix,
                            Href));
                }
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
                    tagBuilder = Generator.GenerateActionLink(linkText: string.Empty,
                                                              actionName: Action,
                                                              controllerName: Controller,
                                                              protocol: Protocol,
                                                              hostname: Host,
                                                              fragment: Fragment,
                                                              routeValues: routeValues,
                                                              htmlAttributes: null);
                }
                else if (Action != null || Controller != null)
                {
                    // Route and Action or Controller were specified. Can't determine the href attribute.
                    throw new InvalidOperationException(
                        Resources.FormatAnchorTagHelper_CannotDetermineHrefRouteActionOrControllerSpecified(
                            "<a>",
                            RouteAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            Href));
                }
                else
                {
                    tagBuilder = Generator.GenerateRouteLink(linkText: string.Empty,
                                                             routeName: Route,
                                                             protocol: Protocol,
                                                             hostName: Host,
                                                             fragment: Fragment,
                                                             routeValues: routeValues,
                                                             htmlAttributes: null);
                }

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);
                }
            }
        }
    }
}