// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class AuthorizationEndpointConventionBuilderExtensions
    {
        public static IEndpointConventionBuilder RequireAuthorization(this IEndpointConventionBuilder builder, params IAuthorizeData[] authorizeData)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (authorizeData == null)
            {
                throw new ArgumentNullException(nameof(authorizeData));
            }

            builder.Add(endpointBuilder =>
            {
                foreach (var data in authorizeData)
                {
                    endpointBuilder.Metadata.Add(data);
                }
            });
            return builder;
        }

        public static IEndpointConventionBuilder RequireAuthorization(this IEndpointConventionBuilder builder, params string[] policyNames)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policyNames == null)
            {
                throw new ArgumentNullException(nameof(policyNames));
            }

            return builder.RequireAuthorization(policyNames.Select(n => new AuthorizeAttribute(n)).ToArray());
        }
    }
}
