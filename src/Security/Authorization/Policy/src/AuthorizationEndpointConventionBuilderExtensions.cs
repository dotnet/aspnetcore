// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Authorization extension methods for <see cref="IEndpointConventionBuilder"/>.
    /// </summary>
    public static class AuthorizationEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Adds the default authorization policy to the endpoint(s).
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <returns>The original convention builder parameter.</returns>
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.RequireAuthorization(new AuthorizeAttribute());
        }

        /// <summary>
        /// Adds authorization policies with the specified names to the endpoint(s).
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="policyNames">A collection of policy names. If empty, the default authorization policy will be used.</param>
        /// <returns>The original convention builder parameter.</returns>
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params string[] policyNames) where TBuilder : IEndpointConventionBuilder
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

        /// <summary>
        /// Adds authorization policies with the specified <see cref="IAuthorizeData"/> to the endpoint(s).
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="authorizeData">
        /// A collection of <paramref name="authorizeData"/>. If empty, the default authorization policy will be used.
        /// </param>
        /// <returns>The original convention builder parameter.</returns>
        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params IAuthorizeData[] authorizeData)
            where TBuilder : IEndpointConventionBuilder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (authorizeData == null)
            {
                throw new ArgumentNullException(nameof(authorizeData));
            }

            if (authorizeData.Length == 0)
            {
                authorizeData = new IAuthorizeData[] { new AuthorizeAttribute(), };
            }

            RequireAuthorizationCore(builder, authorizeData);
            return builder;
        }

        private static void RequireAuthorizationCore<TBuilder>(TBuilder builder, IEnumerable<IAuthorizeData> authorizeData)
            where TBuilder : IEndpointConventionBuilder
        {
            builder.Add(endpointBuilder =>
            {
                foreach (var data in authorizeData)
                {
                    endpointBuilder.Metadata.Add(data);
                }
            });
        }
    }
}
