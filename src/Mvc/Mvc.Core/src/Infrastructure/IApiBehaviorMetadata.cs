// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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
