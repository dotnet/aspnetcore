// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.OptionsModel;

namespace WebApiCompatShimWebSite
{
    public class BasicApiController : ApiController
    {
        [Activate]
        public IOptionsAccessor<WebApiCompatShimOptions> OptionsAccessor { get; set; }

        // Verifies property activation
        [HttpGet]
        public async Task<IActionResult> WriteToHttpContext()
        {
            var message = string.Format(
                "Hello, {0} from {1}",
                User.Identity?.Name ?? "Anonymous User",
                ActionContext.ActionDescriptor.DisplayName);

            await Context.Response.WriteAsync(message);
            return new EmptyResult();
        }

        // Verifies property activation
        [HttpGet]
        public async Task<IActionResult> GenerateUrl()
        {
            var message = string.Format("Visited: {0}", Url.Action());

            await Context.Response.WriteAsync(message);
            return new EmptyResult();
        }

        // Verifies the default options configure formatters correctly.
        [HttpGet]
        public string[] GetFormatters()
        {
            return OptionsAccessor.Options.Formatters.Select(f => f.GetType().FullName).ToArray();
        }
    }
}