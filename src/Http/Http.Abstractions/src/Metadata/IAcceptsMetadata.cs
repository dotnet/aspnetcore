// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Metadata
{
    /// <summary>
    /// Interface for accepting request media types.
    /// </summary>
    public interface IAcceptsMetadata
    {
        /// <summary>
        /// Gets a list of request content types.
        /// </summary>
        IReadOnlyList<string> ContentTypes { get; }
    }
}
