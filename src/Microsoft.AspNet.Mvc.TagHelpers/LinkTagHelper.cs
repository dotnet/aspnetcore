// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;link&gt; elements that supports fallback href paths.
    /// </summary>
    public class LinkTagHelper : TagHelper
    {
        private const string FallbackHrefAttributeName = "asp-fallback-href";
        private const string FallbackTestClassAttributeName = "asp-fallback-test-class";
        private const string FallbackTestPropertyAttributeName = "asp-fallback-test-property";
        private const string FallbackTestValueAttributeName = "asp-fallback-test-value";
        private const string FallbackTestMetaTemplate = "<meta name=\"x-stylesheet-fallback-test\" class=\"{0}\" />";
        private const string FallbackJavaScriptResourceName = "compiler/resources/LinkTagHelper_FallbackJavaScript.js";

        // NOTE: All attributes are required for the LinkTagHelper to process.
        private static readonly string[] RequiredAttributes = new[]
        {
            FallbackHrefAttributeName,
            FallbackTestClassAttributeName,
            FallbackTestPropertyAttributeName,
            FallbackTestValueAttributeName
        };

        /// <summary>
        /// The URL of a CSS stylesheet to fallback to in the case the primary one fails (as specified in the href
        /// attribute).
        /// </summary>
        [HtmlAttributeName(FallbackHrefAttributeName)]
        public string FallbackHref { get; set; }

        /// <summary>
        /// The class name defined in the stylesheet to use for the fallback test.
        /// </summary>
        [HtmlAttributeName(FallbackTestClassAttributeName)]
        public string FallbackTestClass { get; set; }

        /// <summary>
        /// The CSS property name to use for the fallback test.
        /// </summary>
        [HtmlAttributeName(FallbackTestPropertyAttributeName)]
        public string FallbackTestProperty { get; set; }

        /// <summary>
        /// The CSS property value to use for the fallback test.
        /// </summary>
        [HtmlAttributeName(FallbackTestValueAttributeName)]
        public string FallbackTestValue { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ILogger<LinkTagHelper> Logger { get; set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!context.AllRequiredAttributesArePresent(RequiredAttributes, Logger))
            {
                if (Logger.IsEnabled(LogLevel.Verbose))
                {
                    Logger.WriteVerbose("Skipping processing for {0} {1}", nameof(LinkTagHelper), context.UniqueId);
                }
                return;
            }

            var content = new StringBuilder();

            // NOTE: Values in TagHelperOutput.Attributes are already HtmlEncoded

            // We've taken over rendering here so prevent the element rendering the outer tag
            output.TagName = null;

            // Rebuild the <link /> tag that loads the primary stylesheet
            content.Append("<link ");
            foreach (var attribute in output.Attributes)
            {
                content.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\" ", attribute.Key, attribute.Value);
            }
            content.AppendLine("/>");

            // Build the <meta /> tag that's used to test for the presence of the stylesheet
            content.AppendLine(string.Format(CultureInfo.InvariantCulture, FallbackTestMetaTemplate, FallbackTestClass));

            // Build the <script /> tag that checks the effective style of <meta /> tag above and renders the extra
            // <link /> tag to load the fallback stylesheet if the test CSS property value is found to be false,
            // indicating that the primary stylesheet failed to load.
            content.Append("<script>");
            content.AppendFormat(CultureInfo.InvariantCulture,
                                 JavaScriptUtility.GetEmbeddedJavaScript(FallbackJavaScriptResourceName),
                                 JavaScriptUtility.JavaScriptStringEncode(FallbackTestProperty),
                                 JavaScriptUtility.JavaScriptStringEncode(FallbackTestValue),
                                 JavaScriptUtility.JavaScriptStringEncode(FallbackHref));
            content.Append("</script>");

            output.Content = content.ToString();
        }
    }
}