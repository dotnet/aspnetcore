// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal static class ErrorMessageHelper
{
    internal static string BuildErrorMessage(string message, Exception exception, bool? includeExceptionDetails)
    {
        if (includeExceptionDetails ?? false)
        {
            return message + " " + CommonGrpcProtocolHelpers.ConvertToRpcExceptionMessage(exception);
        }

        return message;
    }
}
