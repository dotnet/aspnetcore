// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources;

/// <summary>
/// Contains details for individual exception messages.
/// </summary>
internal sealed class ExceptionDetails
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
