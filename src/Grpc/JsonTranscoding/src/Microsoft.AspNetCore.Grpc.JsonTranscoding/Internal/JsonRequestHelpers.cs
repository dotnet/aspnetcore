// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Google.Api;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal static class JsonRequestHelpers
{
    public const string JsonContentType = "application/json";
    public const string JsonContentTypeWithCharset = "application/json; charset=utf-8";

    public const string StatusDetailsTrailerName = "grpc-status-details-bin";

    public static bool HasJsonContentType(HttpRequest request, out StringSegment charset)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt))
        {
            charset = default;
            return false;
        }

        // Matches application/json
        if (mt.MediaType.Equals(JsonContentType, StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        // Matches +json, e.g. application/ld+json
        if (mt.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        charset = default;
        return false;
    }

    public static (Stream stream, bool usesTranscodingStream) GetStream(Stream innerStream, Encoding? encoding)
    {
        if (encoding == null || encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return (innerStream, false);
        }

        var stream = Encoding.CreateTranscodingStream(innerStream, encoding, Encoding.UTF8, leaveOpen: true);
        return (stream, true);
    }

    public static Encoding? GetEncodingFromCharset(StringSegment charset)
    {
        if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            // This is an optimization for utf-8 that prevents the Substring caused by
            // charset.Value
            return Encoding.UTF8;
        }

        try
        {
            // charset.Value might be an invalid encoding name as in charset=invalid.
            return charset.HasValue ? Encoding.GetEncoding(charset.Value) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to read the request as JSON because the request content type charset '{charset}' is not a known encoding.", ex);
        }
    }

    public static async ValueTask SendErrorResponse(HttpResponse response, Encoding encoding, Metadata trailers, Status status, JsonSerializerOptions options)
    {
        if (!response.HasStarted)
        {
            response.StatusCode = MapStatusCodeToHttpStatus(status.StatusCode);
            response.ContentType = MediaType.ReplaceEncoding("application/json", encoding);
        }

        var e = GetStatusDetails(trailers) ?? new Google.Rpc.Status
        {
            Message = status.Detail,
            Code = (int)status.StatusCode
        };

        await WriteResponseMessage(response, encoding, e, options, CancellationToken.None);

        static Google.Rpc.Status? GetStatusDetails(Metadata trailers)
        {
            var statusDetails = trailers.Get(StatusDetailsTrailerName);
            if (statusDetails?.IsBinary == true)
            {
                try
                {
                    return Google.Rpc.Status.Parser.ParseFrom(statusDetails.ValueBytes);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error when parsing the '{StatusDetailsTrailerName}' trailer.", ex);
                }
            }

            return null;
        }
    }

    public static int MapStatusCodeToHttpStatus(StatusCode statusCode)
    {
        switch (statusCode)
        {
            case StatusCode.OK:
                return StatusCodes.Status200OK;
            case StatusCode.Cancelled:
                return StatusCodes.Status408RequestTimeout;
            case StatusCode.Unknown:
                return StatusCodes.Status500InternalServerError;
            case StatusCode.InvalidArgument:
                return StatusCodes.Status400BadRequest;
            case StatusCode.DeadlineExceeded:
                return StatusCodes.Status504GatewayTimeout;
            case StatusCode.NotFound:
                return StatusCodes.Status404NotFound;
            case StatusCode.AlreadyExists:
                return StatusCodes.Status409Conflict;
            case StatusCode.PermissionDenied:
                return StatusCodes.Status403Forbidden;
            case StatusCode.Unauthenticated:
                return StatusCodes.Status401Unauthorized;
            case StatusCode.ResourceExhausted:
                return StatusCodes.Status429TooManyRequests;
            case StatusCode.FailedPrecondition:
                // Note, this deliberately doesn't translate to the similarly named '412 Precondition Failed' HTTP response status.
                return StatusCodes.Status400BadRequest;
            case StatusCode.Aborted:
                return StatusCodes.Status409Conflict;
            case StatusCode.OutOfRange:
                return StatusCodes.Status400BadRequest;
            case StatusCode.Unimplemented:
                return StatusCodes.Status501NotImplemented;
            case StatusCode.Internal:
                return StatusCodes.Status500InternalServerError;
            case StatusCode.Unavailable:
                return StatusCodes.Status503ServiceUnavailable;
            case StatusCode.DataLoss:
                return StatusCodes.Status500InternalServerError;
        }

        return StatusCodes.Status500InternalServerError;
    }

    public static async ValueTask WriteResponseMessage(HttpResponse response, Encoding encoding, object responseBody, JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        var (stream, usesTranscodingStream) = GetStream(response.Body, encoding);

        try
        {
            await JsonSerializer.SerializeAsync(stream, responseBody, options, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public static async ValueTask<TRequest> ReadMessage<TRequest>(JsonTranscodingServerCallContext serverCallContext, JsonSerializerOptions serializerOptions) where TRequest : class
    {
        try
        {
            GrpcServerLog.ReadingMessage(serverCallContext.Logger);

            IMessage requestMessage;
            if (serverCallContext.DescriptorInfo.BodyDescriptor != null)
            {
                Type type;
                object bodyContent;

                if (serverCallContext.DescriptorInfo.BodyDescriptor.FullName == HttpBody.Descriptor.FullName)
                {
                    type = typeof(HttpBody);

                    bodyContent = await ReadHttpBodyAsync(serverCallContext);
                }
                else
                {
                    if (!serverCallContext.IsJsonRequestContent)
                    {
                        GrpcServerLog.UnsupportedRequestContentType(serverCallContext.Logger, serverCallContext.HttpContext.Request.ContentType);
                        throw new InvalidOperationException($"Unable to read the request as JSON because the request content type '{serverCallContext.HttpContext.Request.ContentType}' is not a known JSON content type.");
                    }

                    var (stream, usesTranscodingStream) = GetStream(serverCallContext.HttpContext.Request.Body, serverCallContext.RequestEncoding);

                    try
                    {
                        if (serverCallContext.DescriptorInfo.BodyDescriptorRepeated)
                        {
                            requestMessage = (IMessage)Activator.CreateInstance<TRequest>();

                            // TODO: JsonSerializer currently doesn't support deserializing values onto an existing object or collection.
                            // Either update this to use new functionality in JsonSerializer or improve work-around perf.
                            type = JsonConverterHelper.GetFieldType(serverCallContext.DescriptorInfo.BodyFieldDescriptor);

                            var args = type.GetGenericArguments();
                            if (serverCallContext.DescriptorInfo.BodyFieldDescriptor.IsMap)
                            {
                                type = typeof(Dictionary<,>).MakeGenericType(args[0], args[1]);
                            }
                            else
                            {
                                type = typeof(List<>).MakeGenericType(args[0]);
                            }

                            GrpcServerLog.DeserializingMessage(serverCallContext.Logger, type);

                            bodyContent = (await JsonSerializer.DeserializeAsync(stream, type, serializerOptions))!;

                            if (bodyContent == null)
                            {
                                throw new InvalidOperationException($"Unable to deserialize null to {type.Name}.");
                            }
                        }
                        else
                        {
                            type = serverCallContext.DescriptorInfo.BodyDescriptor.ClrType;

                            GrpcServerLog.DeserializingMessage(serverCallContext.Logger, type);
                            bodyContent = (IMessage)(await JsonSerializer.DeserializeAsync(stream, serverCallContext.DescriptorInfo.BodyDescriptor.ClrType, serializerOptions))!;
                        }
                    }
                    finally
                    {
                        if (usesTranscodingStream)
                        {
                            await stream.DisposeAsync();
                        }
                    }
                }

                if (serverCallContext.DescriptorInfo.BodyFieldDescriptor != null)
                {
                    requestMessage = (IMessage)Activator.CreateInstance<TRequest>();

                    // The spec says that request body must be on the top-level message.
                    // Recursive request body isn't supported.
                    ServiceDescriptorHelpers.SetValue(requestMessage, serverCallContext.DescriptorInfo.BodyFieldDescriptor, bodyContent);
                }
                else
                {
                    if (bodyContent == null)
                    {
                        throw new InvalidOperationException($"Unable to deserialize null to {type.Name}.");
                    }

                    requestMessage = (IMessage)bodyContent;
                }
            }
            else
            {
                requestMessage = (IMessage)Activator.CreateInstance<TRequest>();
            }

            foreach (var parameterDescriptor in serverCallContext.DescriptorInfo.RouteParameterDescriptors)
            {
                var routeValue = serverCallContext.HttpContext.Request.RouteValues[parameterDescriptor.Key];
                if (routeValue != null)
                {
                    ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, parameterDescriptor.Value.DescriptorsPath, routeValue);
                }
            }

            foreach (var item in serverCallContext.HttpContext.Request.Query)
            {
                if (CanBindQueryStringVariable(serverCallContext, item.Key))
                {
                    var pathDescriptors = GetPathDescriptors(serverCallContext, requestMessage, item.Key);

                    if (pathDescriptors != null)
                    {
                        var value = item.Value.Count == 1 ? (object?)item.Value[0] : item.Value;
                        ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, pathDescriptors, value);
                    }
                }
            }

            GrpcServerLog.ReceivedMessage(serverCallContext.Logger);
            return (TRequest)requestMessage;
        }
        catch (JsonException ex)
        {
            GrpcServerLog.ErrorReadingMessage(serverCallContext.Logger, ex);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Request JSON payload is not correctly formatted.", ex));
        }
        catch (Exception ex)
        {
            GrpcServerLog.ErrorReadingMessage(serverCallContext.Logger, ex);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message, ex));
        }
    }

    private static async ValueTask<IMessage> ReadHttpBodyAsync(JsonTranscodingServerCallContext serverCallContext)
    {
        var httpBody = (IMessage)Activator.CreateInstance(serverCallContext.DescriptorInfo.BodyDescriptor!.ClrType)!;

        var contentType = serverCallContext.HttpContext.Request.ContentType;
        if (contentType != null)
        {
            httpBody.Descriptor.Fields[HttpBody.ContentTypeFieldNumber].Accessor.SetValue(httpBody, contentType);
        }

        var data = await ReadDataAsync(serverCallContext);
        httpBody.Descriptor.Fields[HttpBody.DataFieldNumber].Accessor.SetValue(httpBody, UnsafeByteOperations.UnsafeWrap(data));

        return httpBody;
    }

    private static async ValueTask<byte[]> ReadDataAsync(JsonTranscodingServerCallContext serverCallContext)
    {
        // Buffer to disk if content is larger than 30Kb.
        // Based on value in XmlSerializer and NewtonsoftJson input formatters.
        const int DefaultMemoryThreshold = 1024 * 30;

        var memoryThreshold = DefaultMemoryThreshold;
        var contentLength = serverCallContext.HttpContext.Request.ContentLength.GetValueOrDefault();
        if (contentLength > 0 && contentLength < memoryThreshold)
        {
            // If the Content-Length is known and is smaller than the default buffer size, use it.
            memoryThreshold = (int)contentLength;
        }

        using var fs = new FileBufferingReadStream(serverCallContext.HttpContext.Request.Body, memoryThreshold);

        // Read the request body into buffer.
        // No explicit cancellation token. Request body uses underlying request aborted token.
        await fs.DrainAsync(CancellationToken.None);
        fs.Seek(0, SeekOrigin.Begin);

        var data = new byte[fs.Length];
        var read = fs.Read(data);
        Debug.Assert(read == data.Length);

        return data;
    }

    private static List<FieldDescriptor>? GetPathDescriptors(JsonTranscodingServerCallContext serverCallContext, IMessage requestMessage, string path)
    {
        return serverCallContext.DescriptorInfo.PathDescriptorsCache.GetOrAdd(path, p =>
        {
            ServiceDescriptorHelpers.TryResolveDescriptors(requestMessage.Descriptor, p.Split('.'), allowJsonName: true, out var pathDescriptors);
            return pathDescriptors;
        });
    }

    public static async ValueTask SendMessage<TResponse>(JsonTranscodingServerCallContext serverCallContext, JsonSerializerOptions serializerOptions, TResponse message, CancellationToken cancellationToken) where TResponse : class
    {
        var response = serverCallContext.HttpContext.Response;

        try
        {
            GrpcServerLog.SendingMessage(serverCallContext.Logger);

            object responseBody;
            Type responseType;

            if (serverCallContext.DescriptorInfo.ResponseBodyDescriptor != null)
            {
                // The spec says that response body must be on the top-level message.
                // Recursive response body isn't supported.
                responseBody = serverCallContext.DescriptorInfo.ResponseBodyDescriptor.Accessor.GetValue((IMessage)message);
                responseType = JsonConverterHelper.GetFieldType(serverCallContext.DescriptorInfo.ResponseBodyDescriptor);
            }
            else
            {
                responseBody = message;
                responseType = message.GetType();
            }

            await JsonRequestHelpers.WriteResponseMessage(response, serverCallContext.RequestEncoding, responseBody, serializerOptions, cancellationToken);

            GrpcServerLog.SerializedMessage(serverCallContext.Logger, responseType);
            GrpcServerLog.MessageSent(serverCallContext.Logger);
        }
        catch (Exception ex)
        {
            GrpcServerLog.ErrorSendingMessage(serverCallContext.Logger, ex);
            throw;
        }
    }

    private static bool CanBindQueryStringVariable(JsonTranscodingServerCallContext serverCallContext, string variable)
    {
        if (serverCallContext.DescriptorInfo.BodyDescriptor != null)
        {
            var bodyFieldName = serverCallContext.DescriptorInfo.BodyFieldDescriptor?.Name;

            // Null field name indicates "*" which means the entire message is bound to the body.
            if (bodyFieldName == null)
            {
                return false;
            }

            // Exact match
            if (variable == bodyFieldName)
            {
                return false;
            }

            // Nested field of field name.
            if (bodyFieldName.Length + 1 < variable.Length &&
                variable.StartsWith(bodyFieldName, StringComparison.Ordinal) &&
                variable[bodyFieldName.Length] == '.')
            {
                return false;
            }
        }

        if (serverCallContext.DescriptorInfo.RouteParameterDescriptors.ContainsKey(variable))
        {
            return false;
        }

        return true;
    }
}
