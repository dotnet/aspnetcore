// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public static class HttpResponseExtensions
    {
        public static Task WriteResponseBodyAsync(this HttpResponse response, byte[] buffer, int start, int length, bool isPipeTest = true, CancellationToken token = default)
        {
            if (isPipeTest)
            {
                return response.BodyPipe.WriteAsync(new ReadOnlyMemory<byte>(buffer, start, length), token).AsTask();
            }
            else
            {
                return response.Body.WriteAsync(buffer, start, length, token);
            }
        }

        public static Task WriteResponseAsync(this HttpResponse response, string responseString, bool isPipeTest = true, CancellationToken token = default)
        {
            if (isPipeTest)
            {
                return response.BodyPipe.WriteAsync(Encoding.ASCII.GetBytes(responseString), token).AsTask();
            }
            else
            {
                return response.WriteAsync(responseString, token);
            }
        }

        public static Task FlushResponseAsync(this HttpResponse response, bool isPipeTest = true, CancellationToken token = default)
        {
            if (isPipeTest)
            {
                return response.BodyPipe.FlushAsync(token).AsTask();
            }
            else
            {
                return response.Body.FlushAsync(token);
            }
        }

        public static void WriteResponse(this HttpResponse response, byte[] buffer, int start, int length, bool isPipeTest = true)
        {
            if (isPipeTest)
            {
                response.BodyPipe.WriteAsync(new ReadOnlyMemory<byte>(buffer, start, length)).AsTask().GetAwaiter().GetResult();
            }
            else
            {
                response.Body.Write(buffer, start, length);
            }
        }
    }
}
