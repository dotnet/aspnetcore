// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// An <see cref="IViewComponentResult"/> which writes an <see cref="IHtmlContent"/> when executed.
    /// </summary>
    /// <remarks>
    /// The provided content will be HTML-encoded as specified when the content was created. To encoded and write
    /// text, use a <see cref="ContentViewComponentResult"/>.
    /// </remarks>
    public class HtmlContentViewComponentResult : IViewComponentResult
    {
        /// <summary>
        /// Initializes a new <see cref="HtmlContentViewComponentResult"/>.
        /// </summary>
        public HtmlContentViewComponentResult(IHtmlContent encodedContent)
        {
            if (encodedContent == null)
            {
                throw new ArgumentNullException(nameof(encodedContent));
            }

            EncodedContent = encodedContent;
        }

        /// <summary>
        /// Gets the encoded content.
        /// </summary>
        public IHtmlContent EncodedContent { get; }

        /// <summary>
        /// Writes the <see cref="EncodedContent"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        public void Execute(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Writer.Write(EncodedContent);
        }

        /// <summary>
        /// Writes the <see cref="EncodedContent"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public Task ExecuteAsync(ViewComponentContext context)
        {
            Execute(context);

            return Task.CompletedTask;
        }
    }
}
