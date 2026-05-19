// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Represents an entity which can provide model name as metadata.
/// </summary>
public interface IModelNameProvider
{
    /// <summary>
    /// Model name.
    /// </summary>
    string? Name { get; }
}
