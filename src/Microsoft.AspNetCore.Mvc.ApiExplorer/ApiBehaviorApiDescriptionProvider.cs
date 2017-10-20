// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiBehaviorApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly IModelMetadataProvider _modelMetadaProvider;

        public ApiBehaviorApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadaProvider = modelMetadataProvider;
        }

        /// <remarks>
        /// The order is set to execute after the default provider.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var description in context.Results)
            {
                if (!AppliesTo(description))
                {
                    continue;
                }

                foreach (var responseType in CreateProblemResponseTypes(description))
                {
                    description.SupportedResponseTypes.Add(responseType);
                }
            }
        }

        public bool AppliesTo(ApiDescription description)
        {
            return description.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is IApiBehaviorMetadata);
        }

        // Check if the parameter is named "id" (e.g. int id) or ends in Id (e.g. personId)
        public bool IsIdParameter(ParameterDescriptor parameter)
        {
            if (parameter.Name == null)
            {
                return false;
            }

            if (string.Equals("id", parameter.Name, StringComparison.Ordinal))
            {
                return true;
            }

            // We're looking for a name ending with Id, but preceded by a lower case letter. This should match
            // the normal PascalCase naming conventions.
            if (parameter.Name.Length >= 3 &&
                parameter.Name.EndsWith("Id", StringComparison.Ordinal) &&
                char.IsLower(parameter.Name, parameter.Name.Length - 3))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<ApiResponseType> CreateProblemResponseTypes(ApiDescription description)
        {
            if (description.ActionDescriptor.Parameters.Any() || description.ActionDescriptor.BoundProperties.Any())
            {
                // For validation errors.
                yield return CreateProblemResponse(StatusCodes.Status400BadRequest);

                if (description.ActionDescriptor.Parameters.Any(p => IsIdParameter(p)))
                {
                    yield return CreateProblemResponse(StatusCodes.Status404NotFound);
                }
            }

            yield return CreateProblemResponse(statusCode: 0, isDefaultResponse: true);
        }

        private ApiResponseType CreateProblemResponse(int statusCode, bool isDefaultResponse = false)
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
                IsDefaultResponse = isDefaultResponse,
                ModelMetadata = _modelMetadaProvider.GetMetadataForType(typeof(ProblemDetails)),
                StatusCode = statusCode,
                Type = typeof(ProblemDetails),
            };
        }
    }
}
