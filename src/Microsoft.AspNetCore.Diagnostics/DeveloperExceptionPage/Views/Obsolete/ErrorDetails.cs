// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Diagnostics.Views
{
    /// <summary>
    /// Contains details for individual exception messages.
    /// </summary>
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class ErrorDetails
    {
        /// <summary>
        /// An individual exception
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The generated stack frames
        /// </summary>
        public IEnumerable<StackFrame> StackFrames { get; set; }

        /// <summary>
        /// Gets or sets the summary message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
