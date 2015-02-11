// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
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
        IDictionary<string, object> LoadTempData([NotNull] HttpContext context);

        /// <summary>
        /// Saves the temporary data.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="values">The values to save.</param>
        void SaveTempData([NotNull] HttpContext context, IDictionary<string, object> values);
    }
}