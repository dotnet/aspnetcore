// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Metadata
{
    /// <summary>
    /// Interface marking attributes that specify a parameter should be bound using the request headers.
    /// </summary>
    public interface IFromHeaderMetadata
    {
        /// <summary>
        /// The request header name.
        /// </summary>
        string? Name { get; }
    }
}
