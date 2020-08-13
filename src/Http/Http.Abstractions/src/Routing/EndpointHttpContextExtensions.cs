// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods to expose Endpoint on HttpContext.
    /// </summary>
    public static class EndpointHttpContextExtensions
    {
        /// <summary>
        /// Extension method for getting the <see cref="Endpoint"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The <see cref="Endpoint"/>.</returns>
        public static Endpoint GetEndpoint(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.Features.Get<IEndpointFeature>()?.Endpoint;
        }

        /// <summary>
        /// Extension method for setting the <see cref="Endpoint"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="endpoint">The <see cref="Endpoint"/>.</param>
        public static void SetEndpoint(this HttpContext context, Endpoint endpoint)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var feature = context.Features.Get<IEndpointFeature>();

            if (endpoint != null)
            {
                if (feature == null)
                {
                    feature = new EndpointFeature();
                    context.Features.Set(feature);
                }

                feature.Endpoint = endpoint;
            }
            else
            {
                if (feature == null)
                {
                    // No endpoint to set and no feature on context. Do nothing
                    return;
                }

                feature.Endpoint = null;
            }
        }

        private class EndpointFeature : IEndpointFeature
        {
            public Endpoint Endpoint { get; set; }
        }
    }
}
