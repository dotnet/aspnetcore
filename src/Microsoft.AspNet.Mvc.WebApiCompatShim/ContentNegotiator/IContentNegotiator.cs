// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Collections.Generic;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// Performs content negotiation.
    /// This is the process of selecting a response writer (formatter) in compliance with header values in the request.
    /// </summary>
    public interface IContentNegotiator
    {
        /// <summary>
        /// Performs content negotiating by selecting the most appropriate <see cref="MediaTypeFormatter"/> out of the
        /// passed in <paramref name="formatters"/> for the given <paramref name="request"/> that can serialize an
        /// object of the given <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// Implementations of this method should call <see cref="MediaTypeFormatter.GetPerRequestFormatterInstance"/>
        /// on the selected <see cref="MediaTypeFormatter">formatter</see> and return the result of that method.
        /// </remarks>
        /// <param name="type">The type to be serialized.</param>
        /// <param name="request">
        /// Request message, which contains the header values used to perform negotiation.
        /// </param>
        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
        /// <returns>
        /// The result of the negotiation containing the most appropriate <see cref="MediaTypeFormatter"/> instance,
        /// or <c>null</c> if there is no appropriate formatter.
        /// </returns>
        ContentNegotiationResult Negotiate(
            Type type,
            HttpRequestMessage request,
            IEnumerable<MediaTypeFormatter> formatters);
    }
}
#endif
