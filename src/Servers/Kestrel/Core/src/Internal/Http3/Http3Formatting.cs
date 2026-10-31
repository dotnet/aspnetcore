// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal static class Http3Formatting
{
    public static string ToFormattedType(Http3FrameType type)
    {
        return type switch
        {
            Http3FrameType.Data => "DATA",
            Http3FrameType.Headers => "HEADERS",
            Http3FrameType.CancelPush => "CANCEL_PUSH",
            Http3FrameType.Settings => "SETTINGS",
            Http3FrameType.PushPromise => "PUSH_PROMISE",
            Http3FrameType.GoAway => "GOAWAY",
            Http3FrameType.MaxPushId => "MAX_PUSH_ID",
            _ => type.ToString()
        };
    }

    public static string ToFormattedErrorCode(Http3ErrorCode errorCode)
    {
        return errorCode switch
        {
            Http3ErrorCode.NoError => "H3_NO_ERROR",
            Http3ErrorCode.ProtocolError => "H3_GENERAL_PROTOCOL_ERROR",
            Http3ErrorCode.InternalError => "H3_INTERNAL_ERROR",
            Http3ErrorCode.StreamCreationError => "H3_STREAM_CREATION_ERROR",
            Http3ErrorCode.ClosedCriticalStream => "H3_CLOSED_CRITICAL_STREAM",
            Http3ErrorCode.UnexpectedFrame => "H3_FRAME_UNEXPECTED",
            Http3ErrorCode.FrameError => "H3_FRAME_ERROR",
            Http3ErrorCode.ExcessiveLoad => "H3_EXCESSIVE_LOAD",
            Http3ErrorCode.IdError => "H3_ID_ERROR",
            Http3ErrorCode.SettingsError => "H3_SETTINGS_ERROR",
            Http3ErrorCode.MissingSettings => "H3_MISSING_SETTINGS",
            Http3ErrorCode.RequestRejected => "H3_REQUEST_REJECTED",
            Http3ErrorCode.RequestCancelled => "H3_REQUEST_CANCELLED",
            Http3ErrorCode.RequestIncomplete => "H3_REQUEST_INCOMPLETE",
            Http3ErrorCode.ConnectError => "H3_CONNECT_ERROR",
            Http3ErrorCode.VersionFallback => "H3_VERSION_FALLBACK",
            _ => errorCode.ToString()
        };
    }
}
