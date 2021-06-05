// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Feature to access the <see cref="Activity"/> associated with a request.
    /// </summary>
    public interface IHttpActivityFeature
    {
        /// <summary>
        /// Returns the <see cref="Activity"/> associated with the current request.
        /// </summary>
        Activity Activity { get; set; }
    }
}
