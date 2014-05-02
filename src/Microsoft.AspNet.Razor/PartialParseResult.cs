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
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Razor
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
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "The singular name is more appropriate here")]
    public enum PartialParseResult
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
