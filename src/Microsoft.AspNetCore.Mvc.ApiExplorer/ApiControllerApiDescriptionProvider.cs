// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiControllerApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly IModelMetadataProvider _modelMetadaProvider;

        public ApiControllerApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadaProvider = modelMetadataProvider;
        }

        /// <remarks>
        /// The order is set to execute after the <see cref="DefaultApiDescriptionProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var apiDescription in context.Results)
            {
                if (!apiDescription.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is IApiBehaviorMetadata))
                {
                    continue;
                }

                var parameters = apiDescription.ActionDescriptor.Parameters.Concat(apiDescription.ActionDescriptor.BoundProperties);
                if (parameters.Any())
                {
                    apiDescription.SupportedResponseTypes.Add(CreateProblemResponse(StatusCodes.Status400BadRequest));

                    if (parameters.Any(p => p.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase)))
                    {
                        apiDescription.SupportedResponseTypes.Add(CreateProblemResponse(StatusCodes.Status404NotFound));
                    }
                }

                // We don't have a good way to signal a "default" response type. We'll use 0 to indicate this until we come up
                // with something better.
                apiDescription.SupportedResponseTypes.Add(CreateProblemResponse(statusCode: 0));
            }
        }

        private ApiResponseType CreateProblemResponse(int statusCode)
        {
            return new ApiResponseType
            {
                ApiResponseFormats = new List<ApiResponseFormat>
                    {
                        new ApiResponseFormat
                        {
                            MediaType = "application/problem+json",
                        },
                        new ApiResponseFormat
                        {
                            MediaType = "application/problem+xml",
                        },
                    },
                ModelMetadata = _modelMetadaProvider.GetMetadataForType(typeof(ProblemDetails)),
                StatusCode = statusCode,
                Type = typeof(ProblemDetails),
            };
        }
    }
}
