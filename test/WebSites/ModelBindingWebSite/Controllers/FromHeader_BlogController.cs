// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    [Route("Blog")]
    public class FromHeader_BlogController : Controller
    {
        // Echo back the header value
        [HttpGet("BindToStringParameter")]
        public object BindToStringParameter([FromHeader] string transactionId)
        {
            return new Result()
            {
                HeaderValue = transactionId,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        // Echo back the header values
        [HttpGet("BindToStringArrayParameter")]
        public object BindToStringArrayParameter([FromHeader] string[] transactionIds)
        {
            return new Result()
            {
                HeaderValues = transactionIds,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        [HttpGet("BindToStringParameter/CustomName")]
        public object BindToStringParameterWithCustomName([FromHeader(Name = "tId")] string transactionId)
        {
            return new Result()
            {
                HeaderValue = transactionId,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        [HttpGet("BindToModel")]
        public object BindToModel(BlogPost blogPost)
        {
            return new Result()
            {
                HeaderValue = blogPost.Title,
                HeaderValues = blogPost.Tags,
                ModelStateErrors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0).Select(kvp => kvp.Key).ToArray(),
            };
        }

        private class Result
        {
            public string HeaderValue { get; set; }

            public string[] HeaderValues { get; set; }

            public string[] ModelStateErrors { get; set; }
        }

        public class BlogPost
        {
            [FromHeader]
            public string Title { get; set; }

            [FromHeader]
            public string[] Tags { get; set; }

            public string Author { get; set; }
        }
    }
}