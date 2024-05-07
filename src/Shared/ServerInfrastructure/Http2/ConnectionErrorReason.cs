// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal enum ConnectionErrorReason
{
    ClientAbort,
    ServerAbort,
    FlowControlWindowExceeded,
    KeepAliveTimeout,
    InsufficientTlsVersion,
    InvalidHandshake,
    InvalidStreamId,
    ReceivedFrameAfterStreamClose,
    ReceivedFrameUnknownStream,
    ReceivedUnsupportedFrame,
    InvalidFrameForState,
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
    Other
}
