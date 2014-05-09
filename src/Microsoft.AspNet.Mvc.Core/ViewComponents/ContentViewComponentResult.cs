// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ContentViewComponentResult : IViewComponentResult
    {
        private readonly HtmlString _encoded;

        public ContentViewComponentResult([NotNull] string content)
        {
            _encoded = new HtmlString(WebUtility.HtmlEncode(content));
        }

        public ContentViewComponentResult([NotNull] HtmlString encoded)
        {
            _encoded = encoded;
        }

        public void Execute([NotNull] ViewComponentContext context)
        {
            context.Writer.Write(_encoded.ToString());
        }

        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            await context.Writer.WriteAsync(_encoded.ToString());
        }
    }
}
