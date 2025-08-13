// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// An interface which can be used to identify a type which provides metadata to disable cors for a resource.
/// </summary>
public interface IDisableCorsAttribute : ICorsMetadata
{
}
