// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
    {

        public ApiBehaviorOptionsSetup()
        {
        }

        public void Configure(ApiBehaviorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.InvalidModelStateResponseFactory = GetInvalidModelStateResponse;

            IActionResult GetInvalidModelStateResponse(ActionContext context)
            {
                var result = new BadRequestObjectResult(context.ModelState);

                result.ContentTypes.Add("application/json");
                result.ContentTypes.Add("application/xml");

                return result;
            }
        }
    }
}
