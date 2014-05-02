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

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectResult : ActionResult
    {
        public RedirectResult(string url)
            : this(url, permanent: false)
        {
        }

        public RedirectResult(string url, bool permanent)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            Permanent = permanent;
            Url = url;
        }

        public bool Permanent { get; private set; }

        public string Url { get; private set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            // It is redirected directly to the input URL.
            // We would use the context to construct the full URL,
            // only when relative URLs are supported. (Issue - WEBFX-202)
            context.HttpContext.Response.Redirect(Url, Permanent);
        }
    }
}