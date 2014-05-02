// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
