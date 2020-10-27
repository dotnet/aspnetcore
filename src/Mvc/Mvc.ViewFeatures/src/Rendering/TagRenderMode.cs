// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Specifies constants for tag rendering modes.
    /// </summary>
    public enum TagRenderMode
    {
        /// <summary>
        /// Normal mode.
        /// </summary>
        Normal,

        /// <summary>
        /// Start tag mode.
        /// </summary>
        StartTag,

        /// <summary>
        /// End tag mode.
        /// </summary>
        EndTag,

        /// <summary>
        /// Self closing mode.
        /// </summary>
        SelfClosing
    }
}
