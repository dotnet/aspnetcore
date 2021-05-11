// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Metadata
{
    /// <summary>
    /// Interface marking attributes that specify a parameter should be bound using the request body.
    /// </summary>
    public interface IFromBodyMetadata
    {
        /// <summary>
        /// Gets whether empty input should be rejected or treated as valid.
        /// </summary>
        bool AllowEmpty => false;
    }
}
