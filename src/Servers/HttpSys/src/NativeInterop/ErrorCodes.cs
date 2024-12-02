// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

// Only the useful subset of WIN32_ERROR
internal static class ErrorCodes
{
    internal const uint ERROR_SUCCESS = 0;
    internal const uint ERROR_FILE_NOT_FOUND = 2;
    internal const uint ERROR_ACCESS_DENIED = 5;
    internal const uint ERROR_SHARING_VIOLATION = 32;
    internal const uint ERROR_HANDLE_EOF = 38;
    internal const uint ERROR_NOT_SUPPORTED = 50;
    internal const uint ERROR_INVALID_PARAMETER = 87;
    internal const uint ERROR_INVALID_NAME = 123;
    internal const uint ERROR_ALREADY_EXISTS = 183;
    internal const uint ERROR_MORE_DATA = 234;
    internal const uint ERROR_OPERATION_ABORTED = 995;
    internal const uint ERROR_IO_PENDING = 997;
    internal const uint ERROR_NOT_FOUND = 1168;
    internal const uint ERROR_CONNECTION_INVALID = 1229;
}
