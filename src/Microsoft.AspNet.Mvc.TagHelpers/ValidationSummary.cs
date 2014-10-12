// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.TagHelpers
{
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
}