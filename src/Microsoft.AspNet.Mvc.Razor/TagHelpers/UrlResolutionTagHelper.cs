// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting elements containing attributes with URL expected values.
    /// </summary>
    /// <remarks>Resolves URLs starting with '~/' (relative to the application's 'webroot' setting) that are not
    /// targeted by other <see cref="ITagHelper"/>s. Runs prior to other <see cref="ITagHelper"/>s to ensure
    /// application-relative URLs are resolved.</remarks>
    [TargetElement("*", Attributes = "itemid")]
    [TargetElement("a", Attributes = "href")]
    [TargetElement("applet", Attributes = "archive")]
    [TargetElement("area", Attributes = "href", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("audio", Attributes = "src")]
    [TargetElement("base", Attributes = "href", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("blockquote", Attributes = "cite")]
    [TargetElement("button", Attributes = "formaction")]
    [TargetElement("del", Attributes = "cite")]
    [TargetElement("embed", Attributes = "src", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("form", Attributes = "action")]
    [TargetElement("html", Attributes = "manifest")]
    [TargetElement("iframe", Attributes = "src")]
    [TargetElement("img", Attributes = "src", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("img", Attributes = "srcset", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("input", Attributes = "src", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("input", Attributes = "formaction", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("ins", Attributes = "cite")]
    [TargetElement("link", Attributes = "href", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("menuitem", Attributes = "icon")]
    [TargetElement("object", Attributes = "archive")]
    [TargetElement("object", Attributes = "data")]
    [TargetElement("q", Attributes = "cite")]
    [TargetElement("script", Attributes = "src")]
    [TargetElement("source", Attributes = "src", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("source", Attributes = "srcset", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("track", Attributes = "src", TagStructure = TagStructure.WithoutEndTag)]
    [TargetElement("video", Attributes = "src")]
    [TargetElement("video", Attributes = "poster")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class UrlResolutionTagHelper : TagHelper
    {
        // Valid whitespace characters defined by the HTML5 spec.
        private static readonly char[] ValidAttributeWhitespaceChars =
            new[] { '\t', '\n', '\u000C', '\r', ' ' };
        private static readonly IReadOnlyDictionary<string, IEnumerable<string>> ElementAttributeLookups =
            new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "a", new[] { "href" } },
                { "applet", new[] { "archive" } },
                { "area", new[] { "href" } },
                { "audio", new[] { "src" } },
                { "base", new[] { "href" } },
                { "blockquote", new[] { "cite" } },
                { "button", new[] { "formaction" } },
                { "del", new[] { "cite" } },
                { "embed", new[] { "src" } },
                { "form", new[] { "action" } },
                { "html", new[] { "manifest" } },
                { "iframe", new[] { "src" } },
                { "img", new[] { "src", "srcset" } },
                { "input", new[] { "src", "formaction" } },
                { "ins", new[] { "cite" } },
                { "link", new[] { "href" } },
                { "menuitem", new[] { "icon" } },
                { "object", new[] { "archive", "data" } },
                { "q", new[] { "cite" } },
                { "script", new[] { "src" } },
                { "source", new[] { "src", "srcset" } },
                { "track", new[] { "src" } },
                { "video", new[] { "poster", "src" } },
            };

        /// <summary>
        /// Creates a new <see cref="UrlResolutionTagHelper"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="htmlEncoder">The <see cref="IHtmlEncoder"/>.</param>
        public UrlResolutionTagHelper(IUrlHelper urlHelper, IHtmlEncoder htmlEncoder)
        {
            UrlHelper = urlHelper;
            HtmlEncoder = htmlEncoder;
        }

        /// <inheritdoc />
        public override int Order
        {
            get
            {
                return DefaultOrder.DefaultFrameworkSortOrder - 999;
            }
        }

        protected IUrlHelper UrlHelper { get; }

        protected IHtmlEncoder HtmlEncoder { get; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            IEnumerable<string> attributeNames;
            if (ElementAttributeLookups.TryGetValue(output.TagName, out attributeNames))
            {
                foreach (var attributeName in attributeNames)
                {
                    ProcessUrlAttribute(attributeName, output);
                }
            }

            // itemid can be present on any HTML element.
            ProcessUrlAttribute("itemid", output);
        }

        /// <summary>
        /// Resolves and updates URL values starting with '~/' (relative to the application's 'webroot' setting) for
        /// <paramref name="output"/>'s <see cref="TagHelperOutput.Attributes"/> whose
        /// <see cref="TagHelperAttribute.Name"/> is <paramref name="attributeName"/>.
        /// </summary>
        /// <param name="attributeName">The attribute name used to lookup values to resolve.</param>
        /// <param name="output">The <see cref="TagHelperOutput"/>.</param>
        protected void ProcessUrlAttribute(string attributeName, TagHelperOutput output)
        {
            IEnumerable<TagHelperAttribute> attributes;
            if (output.Attributes.TryGetAttributes(attributeName, out attributes))
            {
                foreach (var attribute in attributes)
                {
                    string resolvedUrl;

                    var stringValue = attribute.Value as string;
                    if (stringValue != null)
                    {
                        if (TryResolveUrl(stringValue, encodeWebRoot: false, resolvedUrl: out resolvedUrl))
                        {
                            attribute.Value = resolvedUrl;
                        }
                    }
                    else
                    {
                        var htmlStringValue = attribute.Value as HtmlString;
                        if (htmlStringValue != null &&
                            TryResolveUrl(
                                htmlStringValue.ToString(),
                                encodeWebRoot: true,
                                resolvedUrl: out resolvedUrl))
                        {
                            attribute.Value = new HtmlString(resolvedUrl);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to resolve the given <paramref name="url"/> value relative to the application's 'webroot' setting.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <param name="encodeWebRoot">If <c>true</c>, will HTML encode the expansion of '~/'.</param>
        /// <param name="resolvedUrl">Absolute URL beginning with the application's virtual root. <c>null</c> if
        /// <paramref name="url"/> could not be resolved.</param>
        /// <returns><c>true</c> if the <paramref name="url"/> could be resolved; <c>false</c> otherwise.</returns>
        protected bool TryResolveUrl(string url, bool encodeWebRoot, out string resolvedUrl)
        {
            resolvedUrl = null;

            if (url == null)
            {
                return false;
            }

            var trimmedUrl = url.Trim(ValidAttributeWhitespaceChars);

            // Before doing more work, ensure that the URL we're looking at is app relative.
            if (trimmedUrl.Length >= 2 && trimmedUrl[0] == '~' && trimmedUrl[1] == '/')
            {
                var appRelativeUrl = UrlHelper.Content(trimmedUrl);

                if (encodeWebRoot)
                {
                    var postTildeSlashUrlValue = trimmedUrl.Substring(2);

                    if (!appRelativeUrl.EndsWith(postTildeSlashUrlValue, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            Resources.FormatCouldNotResolveApplicationRelativeUrl_TagHelper(
                                url,
                                nameof(IUrlHelper),
                                nameof(IUrlHelper.Content),
                                "removeTagHelper",
                                typeof(UrlResolutionTagHelper).FullName,
                                typeof(UrlResolutionTagHelper).GetTypeInfo().Assembly.GetName().Name));
                    }

                    var applicationPath = appRelativeUrl.Substring(0, appRelativeUrl.Length - postTildeSlashUrlValue.Length);
                    var encodedApplicationPath = HtmlEncoder.HtmlEncode(applicationPath);

                    resolvedUrl = string.Concat(encodedApplicationPath, postTildeSlashUrlValue);
                }
                else
                {
                    resolvedUrl = appRelativeUrl;
                }

                return true;
            }

            return false;
        }
    }
}
