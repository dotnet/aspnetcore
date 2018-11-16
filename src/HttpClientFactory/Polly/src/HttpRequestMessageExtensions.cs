// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Http;
using System;
using System.Net.Http;

namespace Polly
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequestMessage"/> Polly integration.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        internal static readonly string PolicyExecutionContextKey = "PolicyExecutionContext";
        
        /// <summary>
        /// Gets the <see cref="Context"/> associated with the provided <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <returns>The <see cref="Context"/> if set, otherwise <c>null</c>.</returns>
        /// <remarks>
        /// The <see cref="PolicyHttpMessageHandler"/> will attach a context to the <see cref="HttpResponseMessage"/> prior
        /// to executing a <see cref="Policy"/>, if one does not already exist. The <see cref="Context"/> will be provided
        /// to the policy for use inside the <see cref="Policy"/> and in other message handlers.
        /// </remarks>
        public static Context GetPolicyExecutionContext(this HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Properties.TryGetValue(PolicyExecutionContextKey, out var context);
            return context as Context;
        }

        /// <summary>
        /// Sets the <see cref="Context"/> associated with the provided <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="context">The <see cref="Context"/>, may be <c>null</c>.</param>
        /// <remarks>
        /// The <see cref="PolicyHttpMessageHandler"/> will attach a context to the <see cref="HttpResponseMessage"/> prior
        /// to executing a <see cref="Policy"/>, if one does not already exist. The <see cref="Context"/> will be provided
        /// to the policy for use inside the <see cref="Policy"/> and in other message handlers.
        /// </remarks>
        public static void SetPolicyExecutionContext(this HttpRequestMessage request, Context context)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Properties[PolicyExecutionContextKey] = context;
        }
    }
}
