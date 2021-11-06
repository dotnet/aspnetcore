// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Enumerates behavior options of the model binding system.
/// </summary>
public enum BindingBehavior
{
    /// <summary>
    /// The property should be model bound if a value is available from the value provider.
    /// </summary>
    Optional = 0,

    /// <summary>
    /// The property should be excluded from model binding.
    /// </summary>
    Never,

    /// <summary>
    /// The property is required for model binding.
    /// </summary>
    Required
}
