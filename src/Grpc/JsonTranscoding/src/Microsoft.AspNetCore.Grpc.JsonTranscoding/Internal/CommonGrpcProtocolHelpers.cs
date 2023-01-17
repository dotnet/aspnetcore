// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal static class CommonGrpcProtocolHelpers
{
    public static string ConvertToRpcExceptionMessage(Exception ex)
    {
        // RpcException doesn't allow for an inner exception. To ensure the user is getting enough information about the
        // error we will concatenate any inner exception messages together.
        return ex.InnerException == null ? $"{ex.GetType().Name}: {ex.Message}" : BuildErrorMessage(ex);
    }

    private static string BuildErrorMessage(Exception ex)
    {
        // Concatenate inner exceptions messages together.
        var sb = new StringBuilder();
        var first = true;
        Exception? current = ex;
        do
        {
            if (!first)
            {
                sb.Append(' ');
            }
            else
            {
                first = false;
            }
            sb.Append(current.GetType().Name);
            sb.Append(": ");
            sb.Append(current.Message);
        }
        while ((current = current.InnerException) != null);

        return sb.ToString();
    }
}
