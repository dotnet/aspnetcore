// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Represents HttpRequest and HttpResponse headers
    /// </summary>
    public interface IHeaderDictionary : IDictionary<string, StringValues>
    {
        /// <summary>
        /// IHeaderDictionary has a different indexer contract than IDictionary, where it will return StringValues.Empty for missing entries.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The stored value, or StringValues.Empty if the key is not present.</returns>
        new StringValues this[string key] { get; set; }
    }
}
