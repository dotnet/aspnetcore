// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Gateway.Runtime;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal;

internal static class JsonRequestHelpers
{
    public const string JsonContentType = "application/json";
    public const string JsonContentTypeWithCharset = "application/json; charset=utf-8";

    public static bool HasJsonContentType(HttpRequest request, out StringSegment charset)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

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

    public static async Task SendErrorResponse(HttpResponse response, Encoding encoding, Status status, JsonSerializerOptions options)
    {
        if (!response.HasStarted)
        {
            response.StatusCode = MapStatusCodeToHttpStatus(status.StatusCode);
            response.ContentType = MediaType.ReplaceEncoding("application/json", encoding);
        }

        var e = new Error
        {
            Error_ = status.Detail,
            Message = status.Detail,
            Code = (int)status.StatusCode
        };

        await WriteResponseMessage(response, encoding, e, options);
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

    public static async Task WriteResponseMessage(HttpResponse response, Encoding encoding, object responseBody, JsonSerializerOptions options)
    {
        var (stream, usesTranscodingStream) = GetStream(response.Body, encoding);

        try
        {
            await JsonSerializer.SerializeAsync(stream, responseBody, options);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public static async Task<TRequest> ReadMessage<TRequest>(HttpApiServerCallContext serverCallContext, JsonSerializerOptions serializerOptions) where TRequest : class
    {
        try
        {
            GrpcServerLog.ReadingMessage(serverCallContext.Logger);

            IMessage requestMessage;
            if (serverCallContext.DescriptorInfo.BodyDescriptor != null)
            {
                if (!serverCallContext.IsJsonRequestContent)
                {
                    GrpcServerLog.UnsupportedRequestContentType(serverCallContext.Logger, serverCallContext.HttpContext.Request.ContentType);
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Request content-type of application/json is required."));
                }

                var (stream, usesTranscodingStream) = GetStream(serverCallContext.HttpContext.Request.Body, serverCallContext.RequestEncoding);

                try
                {
                    if (serverCallContext.DescriptorInfo.BodyDescriptorRepeated)
                    {
                        requestMessage = (IMessage)Activator.CreateInstance<TRequest>();

                        // TODO: JsonSerializer currently doesn't support deserializing values onto an existing object or collection.
                        // Either update this to use new functionality in JsonSerializer or improve work-around perf.
                        var type = JsonConverterHelper.GetFieldType(serverCallContext.DescriptorInfo.BodyFieldDescriptors.Last());
                        var listType = typeof(List<>).MakeGenericType(type);

                        GrpcServerLog.DeserializingMessage(serverCallContext.Logger, listType);
                        var repeatedContent = (IList)(await JsonSerializer.DeserializeAsync(stream, listType, serializerOptions))!;

                        ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, serverCallContext.DescriptorInfo.BodyFieldDescriptors, repeatedContent);
                    }
                    else
                    {
                        IMessage bodyContent;

                        try
                        {
                            GrpcServerLog.DeserializingMessage(serverCallContext.Logger, serverCallContext.DescriptorInfo.BodyDescriptor.ClrType);
                            bodyContent = (IMessage)(await JsonSerializer.DeserializeAsync(stream, serverCallContext.DescriptorInfo.BodyDescriptor.ClrType, serializerOptions))!;
                        }
                        catch (JsonException)
                        {
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "Request JSON payload is not correctly formatted."));
                        }
                        catch (Exception exception)
                        {
                            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
                        }

                        if (serverCallContext.DescriptorInfo.BodyFieldDescriptors != null)
                        {
                            requestMessage = (IMessage)Activator.CreateInstance<TRequest>();
                            ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, serverCallContext.DescriptorInfo.BodyFieldDescriptors, bodyContent!); // TODO - check nullability
                        }
                        else
                        {
                            requestMessage = bodyContent;
                        }
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
            else
            {
                requestMessage = (IMessage)Activator.CreateInstance<TRequest>();
            }

            foreach (var parameterDescriptor in serverCallContext.DescriptorInfo.RouteParameterDescriptors)
            {
                var routeValue = serverCallContext.HttpContext.Request.RouteValues[parameterDescriptor.Key];
                if (routeValue != null)
                {
                    ServiceDescriptorHelpers.RecursiveSetValue(requestMessage, parameterDescriptor.Value, routeValue);
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
        catch (Exception ex)
        {
            GrpcServerLog.ErrorReadingMessage(serverCallContext.Logger, ex);
            throw;
        }
    }

    private static List<FieldDescriptor>? GetPathDescriptors(HttpApiServerCallContext serverCallContext, IMessage requestMessage, string path)
    {
        return serverCallContext.DescriptorInfo.PathDescriptorsCache.GetOrAdd(path, p =>
        {
            ServiceDescriptorHelpers.TryResolveDescriptors(requestMessage.Descriptor, p, out var pathDescriptors);
            return pathDescriptors;
        });
    }

    public static async Task SendMessage<TResponse>(HttpApiServerCallContext serverCallContext, JsonSerializerOptions serializerOptions, TResponse message) where TResponse : class
    {
        var response = serverCallContext.HttpContext.Response;

        try
        {
            GrpcServerLog.SendingMessage(serverCallContext.Logger);

            object responseBody;
            Type responseType;

            if (serverCallContext.DescriptorInfo.ResponseBodyDescriptor != null)
            {
                // TODO: Support recursive response body?
                responseBody = serverCallContext.DescriptorInfo.ResponseBodyDescriptor.Accessor.GetValue((IMessage)message);
                responseType = JsonConverterHelper.GetFieldType(serverCallContext.DescriptorInfo.ResponseBodyDescriptor);
            }
            else
            {
                responseBody = message;
                responseType = message.GetType();
            }

            await JsonRequestHelpers.WriteResponseMessage(response, serverCallContext.RequestEncoding, responseBody, serializerOptions);

            GrpcServerLog.SerializedMessage(serverCallContext.Logger, responseType);
            GrpcServerLog.MessageSent(serverCallContext.Logger);
        }
        catch (Exception ex)
        {
            GrpcServerLog.ErrorSendingMessage(serverCallContext.Logger, ex);
            throw;
        }
    }

    private static bool CanBindQueryStringVariable(HttpApiServerCallContext serverCallContext, string variable)
    {
        if (serverCallContext.DescriptorInfo.BodyDescriptor != null)
        {
            if (serverCallContext.DescriptorInfo.BodyFieldDescriptors == null || serverCallContext.DescriptorInfo.BodyFieldDescriptors.Count == 0)
            {
                return false;
            }

            if (variable == serverCallContext.DescriptorInfo.BodyFieldDescriptorsPath)
            {
                return false;
            }

            if (variable.StartsWith(serverCallContext.DescriptorInfo.BodyFieldDescriptorsPath!, StringComparison.Ordinal))
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
