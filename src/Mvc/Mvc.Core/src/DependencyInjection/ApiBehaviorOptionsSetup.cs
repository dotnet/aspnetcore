// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
    {
        private ProblemDetailsFactory _problemDetailsFactory;

        public void Configure(ApiBehaviorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.InvalidModelStateResponseFactory = context =>
            {
                // ProblemDetailsFactory depends on the ApiBehaviorOptions instance. We intentionally avoid constructor injecting
                // it in this options setup to to avoid a DI cycle.
                _problemDetailsFactory ??= context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                return ProblemDetailsInvalidModelStateResponse(_problemDetailsFactory, context);
            };

            ConfigureClientErrorMapping(options);
        }

        internal static IActionResult ProblemDetailsInvalidModelStateResponse(ProblemDetailsFactory problemDetailsFactory, ActionContext context)
        {
            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
            ObjectResult result;
            if (problemDetails.Status == 400)
            {
                // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
                result = new BadRequestObjectResult(problemDetails);
            }
            else
            {
                result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status,
                };
            }
            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return result;
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

            options.ClientErrorMapping[500] = new ClientErrorData
            {
                Link = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = Resources.ApiConventions_Title_500,
            };
        }
    }
}
