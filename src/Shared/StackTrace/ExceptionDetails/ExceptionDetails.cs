// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources
{
    /// <summary>
    /// Contains details for individual exception messages.
    /// </summary>
    internal class ExceptionDetails
    {
        public ExceptionDetails(Exception error, IEnumerable<StackFrameSourceCodeInfo> stackFrames)
        {
            Error = error;
            StackFrames = stackFrames;
        }

        public ExceptionDetails(string errorMessage, IEnumerable<StackFrameSourceCodeInfo> stackFrames)
        {
            ErrorMessage = errorMessage;
            StackFrames = stackFrames;
        }

        /// <summary>
        /// An individual exception
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// The generated stack frames
        /// </summary>
        public IEnumerable<StackFrameSourceCodeInfo> StackFrames { get; }

        /// <summary>
        /// Gets or sets the summary message.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
