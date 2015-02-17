// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.WebUtilities.Encoders;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;script&gt; elements that supports fallback src paths.
    /// </summary>
    /// <remarks>
    /// <see cref="FallbackSrc" /> and <see cref="FallbackTestExpression" /> are required to not be
    /// <c>null</c> or empty to process.
    /// </remarks>
    public class ScriptTagHelper : TagHelper
    {
        private const string FallbackSrcAttributeName = "asp-fallback-src";
        private const string FallbackTestExpressionAttributeName = "asp-fallback-test";
        private const string SrcAttributeName = "src";

        // NOTE: All attributes are required for the ScriptTagHelper to process.
        private static readonly string[] RequiredAttributes = new[]
        {
            FallbackSrcAttributeName,
            FallbackTestExpressionAttributeName,
        };

        /// <summary>
        /// The URL of a Script tag to fallback to in the case the primary one fails (as specified in the src
        /// attribute).
        /// </summary>
        [HtmlAttributeName(FallbackSrcAttributeName)]
        public string FallbackSrc { get; set; }

        /// <summary>
        /// The script method defined in the primary script to use for the fallback test.
        /// </summary>
        [HtmlAttributeName(FallbackTestExpressionAttributeName)]
        public string FallbackTestExpression { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ILogger<ScriptTagHelper> Logger { get; set; }

        /// <inheritdoc />
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!AttributeMatcher.AllRequiredAttributesArePresent(context, RequiredAttributes, Logger))
            {
                if (Logger.IsEnabled(LogLevel.Verbose))
                {
                    Logger.WriteVerbose("Skipping processing for {0} {1}", nameof(ScriptTagHelper), context.UniqueId);
                }

                return;
            }

            var content = new StringBuilder();

            // NOTE: Values in Output.Attributes are already HtmlEncoded

            // We've taken over rendering here so prevent the element rendering the outer tag
            output.TagName = null;

            // Rebuild the <script /> tag.
            content.Append("<script");
            foreach (var attribute in output.Attributes)
            {
                content.AppendFormat(CultureInfo.InvariantCulture, " {0}=\"{1}\"", attribute.Key, attribute.Value);
            }

            content.Append(">");

            var originalContent = await context.GetChildContentAsync();
            content.Append(originalContent)
                   .AppendLine("</script>");

            // Build the <script> tag that checks the test method and if it fails, renders the extra script.
            content.Append("<script>(")
                   .Append(FallbackTestExpression)
                   .Append("||document.write(\"<script");

            if (!output.Attributes.ContainsKey("src"))
            {
                AppendSrc(content, "src");
            }

            foreach (var attribute in output.Attributes)
            {
                if (!attribute.Key.Equals(SrcAttributeName, StringComparison.OrdinalIgnoreCase))
                {
                    var encodedKey = JavaScriptStringEncoder.Default.JavaScriptStringEncode(attribute.Key);
                    var encodedValue = JavaScriptStringEncoder.Default.JavaScriptStringEncode(attribute.Value);

                    content.AppendFormat(CultureInfo.InvariantCulture, " {0}=\\\"{1}\\\"", encodedKey, encodedValue);
                }
                else
                {
                    AppendSrc(content, attribute.Key);
                }
            }

            content.Append("><\\/script>\"));</script>");

            output.Content = content.ToString();
        }

        private void AppendSrc(StringBuilder content, string srcKey)
        {
            // Append src attribute in the original place and replace the content the fallback content
            // No need to encode the key because we know it is "src".
            content.Append(" ")
                   .Append(srcKey)
                   .Append("=\\\"")
                   .Append(WebUtility.HtmlEncode(FallbackSrc))
                   .Append("\\\"");
        }
    }
}
