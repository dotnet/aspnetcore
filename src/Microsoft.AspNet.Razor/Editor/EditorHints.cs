// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;

namespace Microsoft.AspNet.Razor.Editor
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

        /// <summary>
        /// Indicates that this span's content contains the path to the layout page for this document.
        /// </summary>
        LayoutPage = 2, // 0000 0010
    }
}
