#region Copyright notice and license

// Copyright 2015-2016 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Google.Protobuf;
using Grpc.Core;
using InteropTestsWebsite;

namespace Grpc.Testing;

// Implementation copied from https://github.com/grpc/grpc/blob/master/src/csharp/Grpc.IntegrationTesting/TestServiceImpl.cs
public class TestServiceImpl : TestService.TestServiceBase
{
    public override Task<Empty> EmptyCall(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new Empty());
    }

    public override async Task<SimpleResponse> UnaryCall(SimpleRequest request, ServerCallContext context)
    {
        await EnsureEchoMetadataAsync(context, request.ResponseCompressed?.Value ?? false);
        EnsureEchoStatus(request.ResponseStatus, context);
        EnsureCompression(request.ExpectCompressed, context);

        var response = new SimpleResponse { Payload = CreateZerosPayload(request.ResponseSize) };
        return response;
    }

    public override async Task StreamingOutputCall(StreamingOutputCallRequest request, IServerStreamWriter<StreamingOutputCallResponse> responseStream, ServerCallContext context)
    {
        await EnsureEchoMetadataAsync(context, request.ResponseParameters.Any(rp => rp.Compressed?.Value ?? false));
        EnsureEchoStatus(request.ResponseStatus, context);

        foreach (var responseParam in request.ResponseParameters)
        {
            responseStream.WriteOptions = !(responseParam.Compressed?.Value ?? false)
                ? new WriteOptions(WriteFlags.NoCompress)
                : null;

            var response = new StreamingOutputCallResponse { Payload = CreateZerosPayload(responseParam.Size) };
            await responseStream.WriteAsync(response);
        }
    }

    public override async Task<StreamingInputCallResponse> StreamingInputCall(IAsyncStreamReader<StreamingInputCallRequest> requestStream, ServerCallContext context)
    {
        await EnsureEchoMetadataAsync(context);

        int sum = 0;
        await requestStream.ForEachAsync(request =>
        {
            EnsureCompression(request.ExpectCompressed, context);

            sum += request.Payload.Body.Length;
            return Task.CompletedTask;
        });
        return new StreamingInputCallResponse { AggregatedPayloadSize = sum };
    }

    public override async Task FullDuplexCall(IAsyncStreamReader<StreamingOutputCallRequest> requestStream, IServerStreamWriter<StreamingOutputCallResponse> responseStream, ServerCallContext context)
    {
        await EnsureEchoMetadataAsync(context);

        await requestStream.ForEachAsync(async request =>
        {
            EnsureEchoStatus(request.ResponseStatus, context);
            foreach (var responseParam in request.ResponseParameters)
            {
                var response = new StreamingOutputCallResponse { Payload = CreateZerosPayload(responseParam.Size) };
                await responseStream.WriteAsync(response);
            }
        });
    }

    public override Task HalfDuplexCall(IAsyncStreamReader<StreamingOutputCallRequest> requestStream, IServerStreamWriter<StreamingOutputCallResponse> responseStream, ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    private static Payload CreateZerosPayload(int size)
    {
        return new Payload { Body = ByteString.CopyFrom(new byte[size]) };
    }

    private static async Task EnsureEchoMetadataAsync(ServerCallContext context, bool enableCompression = false)
    {
        var echoInitialList = context.RequestHeaders.Where((entry) => entry.Key == "x-grpc-test-echo-initial").ToList();

        // Append grpc internal compression header if compression is requested by the client
        if (enableCompression)
        {
            echoInitialList.Add(new Metadata.Entry("grpc-internal-encoding-request", "gzip"));
        }

        if (echoInitialList.Any())
        {
            var entry = echoInitialList.Single();
            await context.WriteResponseHeadersAsync(new Metadata { entry });
        }

        var echoTrailingList = context.RequestHeaders.Where((entry) => entry.Key == "x-grpc-test-echo-trailing-bin").ToList();
        if (echoTrailingList.Any())
        {
            context.ResponseTrailers.Add(echoTrailingList.Single());
        }
    }

    private static void EnsureEchoStatus(EchoStatus responseStatus, ServerCallContext context)
    {
        if (responseStatus != null)
        {
            var statusCode = (StatusCode)responseStatus.Code;
            context.Status = new Status(statusCode, responseStatus.Message);
        }
    }

    private static void EnsureCompression(BoolValue? expectCompressed, ServerCallContext context)
    {
        if (expectCompressed != null)
        {
            // ServerCallContext.RequestHeaders filters out grpc-* headers
            // Get grpc-encoding from HttpContext instead
            var encoding = context.GetHttpContext().Request.Headers.SingleOrDefault(h => string.Equals(h.Key, "grpc-encoding", StringComparison.OrdinalIgnoreCase)).Value.SingleOrDefault();
            if (expectCompressed.Value)
            {
                if (encoding == null || encoding == "identity")
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, string.Empty));
                }
            }
        }
    }
}
