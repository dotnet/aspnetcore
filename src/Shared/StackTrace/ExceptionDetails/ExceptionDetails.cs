// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.StackTrace.Sources
{
    /// <summary>
    /// Contains details for individual exception messages.
    /// </summary>
    internal class ExceptionDetails
    {
        /// <summary>
        /// An individual exception
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The generated stack frames
        /// </summary>
        public IEnumerable<StackFrameSourceCodeInfo> StackFrames { get; set; }

        /// <summary>
        /// Gets or sets the summary message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
