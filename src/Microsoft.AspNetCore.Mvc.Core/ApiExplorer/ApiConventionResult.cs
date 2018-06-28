// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Metadata associated with an action method via API convention.
    /// </summary>
    public sealed class ApiConventionResult
    {
        public ApiConventionResult(IReadOnlyList<IApiResponseMetadataProvider> responseMetadataProviders)
        {
            ResponseMetadataProviders = responseMetadataProviders ??
                throw new ArgumentNullException(nameof(responseMetadataProviders));
        }

        public IReadOnlyList<IApiResponseMetadataProvider> ResponseMetadataProviders { get; }

        internal static bool TryGetApiConvention(
            MethodInfo method,
            ApiConventionTypeAttribute[] apiConventionAttributes,
            out ApiConventionResult result)
        {
            foreach (var attribute in apiConventionAttributes)
            {
                var conventionMethod = GetConventionMethod(method, attribute.ConventionType);
                if (conventionMethod != null)
                {
                    var metadataProviders = conventionMethod.GetCustomAttributes(inherit: false)
                        .OfType<IApiResponseMetadataProvider>()
                        .ToArray();

                    result = new ApiConventionResult(metadataProviders);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static MethodInfo GetConventionMethod(MethodInfo method, Type conventionType)
        {
            foreach (var conventionMethod in conventionType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (ApiConventionMatcher.IsMatch(method, conventionMethod))
                {
                    return conventionMethod;
                }
            }

            return null;
        }
    }
}
