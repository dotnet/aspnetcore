// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ContentViewComponentResult : IViewComponentResult
    {
        public ContentViewComponentResult([NotNull] string content)
        {
            Content = content;
            EncodedContent = new HtmlString(WebUtility.HtmlEncode(content));
        }

        public ContentViewComponentResult([NotNull] HtmlString encodedContent)
        {
            EncodedContent = encodedContent;
            Content = WebUtility.HtmlDecode(encodedContent.ToString());
        }

        public string Content { get; private set; }

        public HtmlString EncodedContent { get; private set; }

        public void Execute([NotNull] ViewComponentContext context)
        {
            context.Writer.Write(EncodedContent.ToString());
        }

        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            await context.Writer.WriteAsync(EncodedContent.ToString());
        }
    }
}
