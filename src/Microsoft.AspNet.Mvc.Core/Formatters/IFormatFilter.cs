// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    ///  A filter which produces a desired content type for the current request. 
    /// </summary>
    public interface IFormatFilter : IFilterMetadata
    {
        /// <summary>
        /// Gets the format value for the request associated with the provided <see cref="ActionContext"/>. 
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <returns>A format value, or <c>null</c> if a format cannot be determined for the request.</returns>
        string GetFormat(ActionContext context);
    }
}