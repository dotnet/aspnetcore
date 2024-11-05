// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Shared;
using Grpc.Shared.Server;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal sealed class JsonTranscodingServerCallContext : ServerCallContext, IServerCallContextFeature
{
    private static readonly AuthContext UnauthenticatedContext = new AuthContext(null, new Dictionary<string, List<AuthProperty>>());

    private readonly IMethod _method;
    private Metadata? _responseTrailers;

    public HttpContext HttpContext { get; }
    public MethodOptions Options { get; }
    public CallHandlerDescriptorInfo DescriptorInfo { get; }
    public bool IsJsonRequestContent { get; private set; }
    // Default request encoding to UTF8 so an encoding is available
    // if the request sends an invalid/unsupported encoding.
    public Encoding RequestEncoding { get; private set; } = Encoding.UTF8;

    internal ILogger Logger { get; }

    private string? _peer;
    private Metadata? _requestHeaders;
    private AuthContext? _authContext;

    public JsonTranscodingServerCallContext(HttpContext httpContext, MethodOptions options, IMethod method, CallHandlerDescriptorInfo descriptorInfo, ILogger logger)
    {
        HttpContext = httpContext;
        Options = options;
        _method = method;
        DescriptorInfo = descriptorInfo;
        Logger = logger;
    }

    public void Initialize()
    {
        IsJsonRequestContent = JsonRequestHelpers.HasJsonContentType(HttpContext.Request, out var charset);
        RequestEncoding = JsonRequestHelpers.GetEncodingFromCharset(charset) ?? Encoding.UTF8;

        // HttpContext.Items is publically exposed as ServerCallContext.UserState.
        // Because this is a custom ServerCallContext, HttpContext must be added to UserState so GetHttpContext() continues to work.
        // https://github.com/grpc/grpc-dotnet/blob/7ef184f3c4cd62fbc3cde55e4bb3e16b58258ca1/src/Grpc.AspNetCore.Server/ServerCallContextExtensions.cs#L53-L61
        HttpContext.Items["__HttpContext"] = HttpContext;
    }

    public ServerCallContext ServerCallContext => this;

    protected override string MethodCore => _method.FullName;

    protected override string HostCore => HttpContext.Request.Host.Value ?? string.Empty;

    protected override string PeerCore
    {
        get
        {
            // Follows the standard at https://github.com/grpc/grpc/blob/master/doc/naming.md
            if (_peer == null)
            {
                _peer = BuildPeer();
            }

            return _peer;
        }
    }

    private string BuildPeer()
    {
        var connection = HttpContext.Connection;
        if (connection.RemoteIpAddress != null)
        {
            switch (connection.RemoteIpAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return $"ipv4:{connection.RemoteIpAddress}:{connection.RemotePort}";
                case AddressFamily.InterNetworkV6:
                    return $"ipv6:[{connection.RemoteIpAddress}]:{connection.RemotePort}";
                default:
                    // TODO(JamesNK) - Test what should be output when used with UDS and named pipes
                    return $"unknown:{connection.RemoteIpAddress}:{connection.RemotePort}";
            }
        }
        else
        {
            return "unknown"; // Match Grpc.Core
        }
    }

    internal async Task ProcessHandlerErrorAsync(Exception ex, string method, bool isStreaming, JsonSerializerOptions options)
    {
        Status status;
        if (ex is RpcException rpcException)
        {
            // RpcException is thrown by client code to modify the status returned from the server.
            // Log the status, detail and debug exception (if present).
            // Don't log the RpcException itself to reduce log verbosity. All of its information is already captured.
            GrpcServerLog.RpcConnectionError(Logger, rpcException.StatusCode, rpcException.Status.Detail, rpcException.Status.DebugException);

            status = rpcException.Status;
            foreach (var entry in rpcException.Trailers)
            {
                ResponseTrailers.Add(entry);
            }
        }
        else
        {
            GrpcServerLog.ErrorExecutingServiceMethod(Logger, method, ex);

            var message = ErrorMessageHelper.BuildErrorMessage("Exception was thrown by handler.", ex, Options.EnableDetailedErrors);

            // Note that the exception given to status won't be returned to the client.
            // It is still useful to set in case an interceptor accesses the status on the server.
            status = new Status(StatusCode.Unknown, message, ex);
        }

        await JsonRequestHelpers.SendErrorResponse(HttpContext.Response, RequestEncoding, ResponseTrailers, status, options);
        if (isStreaming)
        {
            await HttpContext.Response.Body.WriteAsync(GrpcProtocolConstants.StreamingDelimiter);
        }
    }

    // Deadline returns max value when there isn't a deadline.
    protected override DateTime DeadlineCore => DateTime.MaxValue;

    protected override Metadata RequestHeadersCore
    {
        get
        {
            if (_requestHeaders == null)
            {
                _requestHeaders = new Metadata();

                foreach (var header in HttpContext.Request.Headers)
                {
                    // gRPC metadata contains a subset of the request headers
                    // Filter out pseudo headers (start with :) and other known headers
                    if (header.Key.StartsWith(':') || GrpcProtocolConstants.FilteredHeaders.Contains(header.Key))
                    {
                        continue;
                    }
                    else if (header.Key.EndsWith(Metadata.BinaryHeaderSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        _requestHeaders.Add(header.Key, GrpcProtocolHelpers.ParseBinaryHeader(header.Value!));
                    }
                    else
                    {
                        _requestHeaders.Add(header.Key, header.Value!);
                    }
                }
            }

            return _requestHeaders;
        }
    }

    protected override CancellationToken CancellationTokenCore => HttpContext.RequestAborted;

    protected override Metadata ResponseTrailersCore => _responseTrailers ??= new();

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    protected override AuthContext AuthContextCore
    {
        get
        {
            if (_authContext == null)
            {
                var clientCertificate = HttpContext.Connection.ClientCertificate;

                _authContext = clientCertificate == null
                    ? UnauthenticatedContext
                    : AuthContextHelpers.CreateAuthContext(clientCertificate);
            }

            return _authContext;
        }
    }

    protected override IDictionary<object, object> UserStateCore => HttpContext.Items!;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
    {
        throw new NotImplementedException();
    }

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
    {
        // Headers can only be written once. Throw on subsequent call to write response header instead of silent no-op.
        if (HttpContext.Response.HasStarted)
        {
            throw new InvalidOperationException("Response headers can only be sent once per call.");
        }

        if (responseHeaders != null)
        {
            foreach (var entry in responseHeaders)
            {
                if (entry.IsBinary)
                {
                    HttpContext.Response.Headers[entry.Key] = Convert.ToBase64String(entry.ValueBytes);
                }
                else
                {
                    HttpContext.Response.Headers[entry.Key] = entry.Value;
                }
            }
        }

        EnsureResponseHeaders();

        return HttpContext.Response.BodyWriter.FlushAsync().GetAsTask();
    }

    internal void EnsureResponseHeaders(string? contentType = null)
    {
        if (!HttpContext.Response.HasStarted)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            HttpContext.Response.ContentType = contentType ?? MediaType.ReplaceEncoding("application/json", RequestEncoding);
        }
    }
}
