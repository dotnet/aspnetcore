// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal enum ConnectionEndReason
{
    NoError,
    ConnectionReset,
    FlowControlWindowExceeded,
    KeepAliveTimeout,
    InsufficientTlsVersion,
    InvalidHandshake,
    InvalidStreamId,
    FrameAfterStreamClose,
    UnknownStream,
    UnsupportedFrame,
    UnexpectedFrame,
    InvalidFrameLength,
    InvalidDataPadding,
    InvalidRequestHeaders,
    StreamResetLimitExceeded,
    WindowUpdateSizeInvalid,
    StreamSelfDependency,
    InvalidSettings,
    MissingStreamEnd,
    MaxFrameLengthExceeded,
    ErrorReadingHeaders,
    ErrorWritingHeaders,
    UnexpectedError,
    InputOrOutputCompleted,
    InvalidHttpVersion,
    RequestHeadersTimeout,
    MinRequestBodyDataRate,
    MinResponseDataRate,
    FlowControlQueueSizeExceeded,
    OutputQueueSizeExceeded,
    ClosedCriticalStream,
    AbortedByApplication,
    ServerTimeout,
    StreamCreationError,
    IOError,
    ClientGoAway,
    ApplicationShutdown
}
