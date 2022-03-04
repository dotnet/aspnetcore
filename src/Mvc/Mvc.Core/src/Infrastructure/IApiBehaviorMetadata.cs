// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A <see cref="IFilterMetadata"/> that indicates that a type and all derived types are used to serve HTTP API responses.
    /// <para>
    /// Controllers decorated with this attribute (<see cref="ApiControllerAttribute"/>) are configured with
    /// features and behavior targeted at improving the developer experience for building APIs.
    /// </para>
    /// </summary>
    public interface IApiBehaviorMetadata : IFilterMetadata
    {
    }
}
