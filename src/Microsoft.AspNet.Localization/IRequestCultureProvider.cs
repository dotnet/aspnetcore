// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Represents a provider for determining the culture information of an <see cref="HttpRequest"/>.
    /// </summary>
    public interface IRequestCultureProvider
    {
        /// <summary>
        /// Implements the provider to determine the culture of the given request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the request.</param>
        /// <returns>
        ///     The determined <see cref="RequestCulture"/>.
        ///     Returns <c>null</c> if the provider couldn't determine a <see cref="RequestCulture"/>.
        /// </returns>
        Task<RequestCulture> DetermineRequestCulture(HttpContext httpContext);
    }
}
