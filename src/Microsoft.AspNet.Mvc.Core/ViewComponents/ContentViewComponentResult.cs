// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="IViewComponentResult"/> which writes text when executed.
    /// </summary>
    /// <remarks>
    /// <see cref="ContentViewComponentResult"/> always writes HTML encoded text from the
    /// <see cref="EncodedContent"/> property.
    ///
    /// When using <see cref="ContentViewComponentResult(string)"/>, the provided content will be HTML
    /// encoded and stored in <see cref="EncodedContent"/>.
    ///
    /// To write pre-encoded conent, use <see cref="ContentViewComponentResult(HtmlString)"/>.
    /// </remarks>
    public class ContentViewComponentResult : IViewComponentResult
    {
        /// <summary>
        /// Initializes a new <see cref="ContentViewComponentResult"/>.
        /// </summary>
        /// <param name="content">Content to write. The content be HTML encoded when output.</param>
        public ContentViewComponentResult([NotNull] string content)
        {
            Content = content;
            EncodedContent = new HtmlString(WebUtility.HtmlEncode(content));
        }

        /// <summary>
        /// Initializes a new <see cref="ContentViewComponentResult"/>.
        /// </summary>
        /// <param name="content">
        /// Content to write. The content is treated as already HTML encoded, and no further encoding
        /// will be performed.
        /// </param>
        public ContentViewComponentResult([NotNull] HtmlString encodedContent)
        {
            EncodedContent = encodedContent;
            Content = WebUtility.HtmlDecode(encodedContent.ToString());
        }

        /// <summary>
        /// Gets the content.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the encoded content.
        /// </summary>
        public HtmlString EncodedContent { get; }

        /// <summary>
        /// Writes the <see cref="EncodedContent"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        public void Execute([NotNull] ViewComponentContext context)
        {
            context.Writer.Write(EncodedContent.ToString());
        }

        /// <summary>
        /// Writes the <see cref="EncodedContent"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            await context.Writer.WriteAsync(EncodedContent.ToString());
        }
    }
}
