// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Defines the contract for temporary-data providers that store data that is viewed on the next request.
    /// </summary>
    public interface ITempDataProvider
    {
        /// <summary>
        /// Loads the temporary data.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>The temporary data.</returns>
        IDictionary<string, object> LoadTempData(HttpContext context);

        /// <summary>
        /// Saves the temporary data.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="values">The values to save.</param>
        void SaveTempData(HttpContext context, IDictionary<string, object> values);
    }
}