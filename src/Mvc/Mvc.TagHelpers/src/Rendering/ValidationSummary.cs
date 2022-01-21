// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Acceptable validation summary rendering modes.
/// </summary>
public enum ValidationSummary
{
    /// <summary>
    /// No validation summary.
    /// </summary>
    None,

    /// <summary>
    /// Validation summary with model-level errors only (excludes all property errors).
    /// </summary>
    ModelOnly,

    /// <summary>
    /// Validation summary with all errors.
    /// </summary>
    All
}
