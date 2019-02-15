// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ApiBehaviorOptionsSetup :
        IConfigureOptions<ApiBehaviorOptions>,
        IPostConfigureOptions<ApiBehaviorOptions>
    {
        internal static readonly Func<ActionContext, IActionResult> DefaultFactory = DefaultInvalidModelStateResponse;
        internal static readonly Func<ActionContext, IActionResult> ProblemDetailsFactory =
            ProblemDetailsInvalidModelStateResponse;

        public void Configure(ApiBehaviorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.InvalidModelStateResponseFactory = DefaultFactory;
            ConfigureClientErrorMapping(options);
        }

        public void PostConfigure(string name, ApiBehaviorOptions options)
        {
            // We want to use problem details factory only if
            // (a) it has not been opted out of (SuppressMapClientErrors = true)
            // (b) a different factory was configured
            if (!options.SuppressMapClientErrors &&
                object.ReferenceEquals(options.InvalidModelStateResponseFactory, DefaultFactory))
            {
                options.InvalidModelStateResponseFactory = ProblemDetailsFactory;
            }
        }

        // Internal for unit testing
        internal static void ConfigureClientErrorMapping(ApiBehaviorOptions options)
        {
            options.ClientErrorMapping[400] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = Resources.ApiConventions_Title_400,
            };

            options.ClientErrorMapping[401] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = Resources.ApiConventions_Title_401,
            };

            options.ClientErrorMapping[403] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = Resources.ApiConventions_Title_403,
            };

            options.ClientErrorMapping[404] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = Resources.ApiConventions_Title_404,
            };

            options.ClientErrorMapping[406] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.6",
                Title = Resources.ApiConventions_Title_406,
            };

            options.ClientErrorMapping[409] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = Resources.ApiConventions_Title_409,
            };

            options.ClientErrorMapping[415] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.5.13",
                Title = Resources.ApiConventions_Title_415,
            };

            options.ClientErrorMapping[422] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = Resources.ApiConventions_Title_422,
            };
        }

        private static IActionResult DefaultInvalidModelStateResponse(ActionContext context)
        {
            var result = new BadRequestObjectResult(context.ModelState);

            result.ContentTypes.Add("application/json");
            result.ContentTypes.Add("application/xml");

            return result;
        }

        internal static IActionResult ProblemDetailsInvalidModelStateResponse(ActionContext context)
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            };

            ProblemDetailsClientErrorFactory.SetTraceId(context, problemDetails);

            var result = new BadRequestObjectResult(problemDetails);

            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return result;
        }
    }
}
