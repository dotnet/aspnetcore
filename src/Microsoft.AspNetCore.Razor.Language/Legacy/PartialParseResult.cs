// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    // Flags:
    //  Provisional, ContextChanged, Accepted, Rejected
    //  000001 1  - Rejected,
    //  000010 2  - Accepted
    //  000100 4  - Provisional
    //  001000 8  - Context Changed
    //  010000 16 - Auto Complete Block

    /// <summary>
    /// The result of attempting an incremental parse
    /// </summary>
    /// <remarks>
    /// Either the Accepted or Rejected flag is ALWAYS set.
    /// Additionally, Provisional may be set with Accepted and SpanContextChanged may be set with Rejected.
    /// Provisional may NOT be set with Rejected and SpanContextChanged may NOT be set with Accepted.
    /// </remarks>
    [Flags]
    internal enum PartialParseResult
    {
        /// <summary>
        /// Indicates that the edit could not be accepted and that a reparse is underway.
        /// </summary>
        Rejected = 1,

        /// <summary>
        /// Indicates that the edit was accepted and has been added to the parse tree
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// Indicates that the edit was accepted, but that a reparse should be forced when idle time is available
        /// since the edit may be misclassified
        /// </summary>
        /// <remarks>
        /// This generally occurs when a "." is typed in an Implicit Expression, since editors require that this
        /// be assigned to Code in order to properly support features like IntelliSense.  However, if no further edits
        /// occur following the ".", it should be treated as Markup.
        /// </remarks>
        Provisional = 4,

        /// <summary>
        /// Indicates that the edit caused a change in the span's context and that if any statement completions were active prior to starting this
        /// partial parse, they should be reinitialized.
        /// </summary>
        SpanContextChanged = 8,

        /// <summary>
        /// Indicates that the edit requires an auto completion to occur
        /// </summary>
        AutoCompleteBlock = 16
    }
}
