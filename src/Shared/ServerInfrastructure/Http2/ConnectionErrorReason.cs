// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal enum ConnectionErrorReason
{
    NoError,
    ClientAbort,
    FlowControlWindowExceeded,
    KeepAliveTimeout,
    InsufficientTlsVersion,
    InvalidHandshake,
    InvalidStreamId,
    ReceivedFrameAfterStreamClose,
    ReceivedFrameUnknownStream,
    ReceivedUnsupportedFrame,
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
    Other,
    InputOrOutputCompleted,
    InvalidHttpVersion,
    RequestHeadersTimeout,
    RequestBodyTimeout,
    FlowControlQueueSizeExceeded,
    OutputQueueSizeExceeded,
    ClosedCriticalStream,
    ResponseMininumDataRateNotSatisfied,
    AbortedByApplication,
    ServerTimeout,
    StreamCreationError
}
