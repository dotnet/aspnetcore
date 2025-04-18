// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;

/// <summary>
/// Defines the operations used for loading an <see cref="IAdapter"/> based on the current object and ContractResolver.
/// </summary>
internal interface IAdapterFactory
{
    /// <summary>
    /// Creates an <see cref="IAdapter"/> for the current object
    /// </summary>
    /// <param name="target">The target object</param>
    /// <returns>The needed <see cref="IAdapter"/></returns>
    IAdapter Create(object target);
}
