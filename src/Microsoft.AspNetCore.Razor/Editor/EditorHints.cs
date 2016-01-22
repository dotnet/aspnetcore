// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Editor
{
    /// <summary>
    /// Used within <see cref="F:SpanEditHandler.EditorHints"/>.
    /// </summary>
    [Flags]
    public enum EditorHints
    {
        /// <summary>
        /// The default (Markup or Code) editor behavior for Statement completion should be used.
        /// Editors can always use the default behavior, even if the span is labeled with a different CompletionType.
        /// </summary>
        None = 0, // 0000 0000

        /// <summary>
        /// Indicates that Virtual Path completion should be used for this span if the editor supports it.
        /// Editors need not support this mode of completion, and will use the default (<see cref="F:EditorHints.None"/>) behavior
        /// if they do not support it.
        /// </summary>
        VirtualPath = 1, // 0000 0001
    }
}
