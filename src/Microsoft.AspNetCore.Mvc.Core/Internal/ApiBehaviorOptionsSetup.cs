// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiBehaviorOptionsSetup :
        ConfigureCompatibilityOptions<ApiBehaviorOptions>,
        IConfigureOptions<ApiBehaviorOptions>
    {
        internal static readonly Func<ActionContext, IActionResult> DefaultFactory = DefaultInvalidModelStateResponse;
        internal static readonly Func<ActionContext, IActionResult> ProblemDetailsFactory = ProblemDetailsInvalidModelStateResponse;

        public ApiBehaviorOptionsSetup(
            ILoggerFactory loggerFactory,
            IOptions<MvcCompatibilityOptions> compatibilityOptions)
            : base(loggerFactory, compatibilityOptions)
        {
        }

        protected override IReadOnlyDictionary<string, object> DefaultValues
        {
            get
            {
                var dictionary = new Dictionary<string, object>();

                if (Version < CompatibilityVersion.Version_2_2)
                {
                    dictionary[nameof(ApiBehaviorOptions.SuppressUseClientErrorFactory)] = true;
                    dictionary[nameof(ApiBehaviorOptions.SuppressUseValidationProblemDetailsForInvalidModelStateResponses)] = true;
                }

                return dictionary;
            }
        }

        public void Configure(ApiBehaviorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.InvalidModelStateResponseFactory = DefaultFactory;
            ConfigureClientErrorFactories(options);
        }

        public override void PostConfigure(string name, ApiBehaviorOptions options)
        {
            // Let compatibility switches do their thing.
            base.PostConfigure(name, options);

            // We want to use problem details factory only if
            // (a) it has not been opted out of (SuppressUseClientErrorFactory = true)
            // (b) a different factory was configured
            if (!options.SuppressUseClientErrorFactory &&
                object.ReferenceEquals(options.InvalidModelStateResponseFactory, DefaultFactory))
            {
                options.InvalidModelStateResponseFactory = ProblemDetailsFactory;
            }
        }

        // Internal for unit testing
        internal static void ConfigureClientErrorFactories(ApiBehaviorOptions options)
        {
            AddClientErrorFactory(new ProblemDetails
            {
                Status = 400,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = Resources.ApiConventions_Title_400,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 401,
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = Resources.ApiConventions_Title_401,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 403,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = Resources.ApiConventions_Title_403,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 404,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = Resources.ApiConventions_Title_404,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 406,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.6",
                Title = Resources.ApiConventions_Title_406,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 409,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = Resources.ApiConventions_Title_409,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 415,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.13",
                Title = Resources.ApiConventions_Title_415,
            });

            AddClientErrorFactory(new ProblemDetails
            {
                Status = 422,
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = Resources.ApiConventions_Title_422,
            });

            void AddClientErrorFactory(ProblemDetails problemDetails)
            {
                var statusCode = problemDetails.Status.Value;
                options.ClientErrorFactory[statusCode] = _ => new ObjectResult(problemDetails)
                {
                    StatusCode = statusCode,
                    ContentTypes =
                    {
                        "application/problem+json",
                        "application/problem+xml",
                    },
                };
            }
        }

        private static IActionResult DefaultInvalidModelStateResponse(ActionContext context)
        {
            var result = new BadRequestObjectResult(context.ModelState);

            result.ContentTypes.Add("application/json");
            result.ContentTypes.Add("application/xml");

            return result;
        }

        private static IActionResult ProblemDetailsInvalidModelStateResponse(ActionContext context)
        {
            var result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));

            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return result;
        }
    }
}
