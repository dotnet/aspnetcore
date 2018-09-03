// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Metadata that defines data tokens for an <see cref="Endpoint"/>. This metadata
    /// type provides data tokens value for <see cref="RouteData.DataTokens"/> associated
    /// with an endpoint.
    /// </summary>
    public sealed class DataTokensMetadata : IDataTokensMetadata
    {
        public DataTokensMetadata(IReadOnlyDictionary<string, object> dataTokens)
        {
            if (dataTokens == null)
            {
                throw new ArgumentNullException(nameof(dataTokens));
            }

            DataTokens = dataTokens;
        }

        /// <summary>
        /// Get the data tokens.
        /// </summary>
        public IReadOnlyDictionary<string, object> DataTokens { get; }
    }
}
