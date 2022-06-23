// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

public static partial class RequestDelegateFactory
{
    private static partial class Log
    {
        private const string InvalidJsonRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as JSON.";
        private const string InvalidJsonRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as JSON.";

        private const string ParameterBindingFailedLogMessage = @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".";
        private const string ParameterBindingFailedExceptionMessage = @"Failed to bind parameter ""{0} {1}"" from ""{2}"".";

        private const string RequiredParameterNotProvidedLogMessage = @"Required parameter ""{ParameterType} {ParameterName}"" was not provided from {Source}.";
        private const string RequiredParameterNotProvidedExceptionMessage = @"Required parameter ""{0} {1}"" was not provided from {2}.";

        private const string UnexpectedJsonContentTypeLogMessage = @"Expected a supported JSON media type but got ""{ContentType}"".";
        private const string UnexpectedJsonContentTypeExceptionMessage = @"Expected a supported JSON media type but got ""{0}"".";

        private const string ImplicitBodyNotProvidedLogMessage = @"Implicit body inferred for parameter ""{ParameterName}"" but no body was provided. Did you mean to use a Service instead?";
        private const string ImplicitBodyNotProvidedExceptionMessage = @"Implicit body inferred for parameter ""{0}"" but no body was provided. Did you mean to use a Service instead?";

        private const string InvalidFormRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as form.";
        private const string InvalidFormRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as form.";

        private const string UnexpectedFormContentTypeLogMessage = @"Expected a supported form media type but got ""{ContentType}"".";
        private const string UnexpectedFormContentTypeExceptionMessage = @"Expected a supported form media type but got ""{0}"".";

        // This doesn't take a shouldThrow parameter because an IOException indicates an aborted request rather than a "bad" request so
        // a BadHttpRequestException feels wrong. The client shouldn't be able to read the Developer Exception Page at any rate.
        public static void RequestBodyIOException(HttpContext httpContext, IOException exception)
            => RequestBodyIOException(GetLogger(httpContext), exception);

        [LoggerMessage(1, LogLevel.Debug, "Reading the request body failed with an IOException.", EventName = "RequestBodyIOException")]
        private static partial void RequestBodyIOException(ILogger logger, IOException exception);

        public static void InvalidJsonRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, InvalidJsonRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidJsonRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(2, LogLevel.Debug, InvalidJsonRequestBodyMessage, EventName = "InvalidJsonRequestBody")]
        private static partial void InvalidJsonRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void ParameterBindingFailed(HttpContext httpContext, string parameterTypeName, string parameterName, string sourceValue, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, ParameterBindingFailedExceptionMessage, parameterTypeName, parameterName, sourceValue);
                throw new BadHttpRequestException(message);
            }

            ParameterBindingFailed(GetLogger(httpContext), parameterTypeName, parameterName, sourceValue);
        }

        [LoggerMessage(3, LogLevel.Debug, ParameterBindingFailedLogMessage, EventName = "ParameterBindingFailed")]
        private static partial void ParameterBindingFailed(ILogger logger, string parameterType, string parameterName, string sourceValue);

        public static void RequiredParameterNotProvided(HttpContext httpContext, string parameterTypeName, string parameterName, string source, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequiredParameterNotProvidedExceptionMessage, parameterTypeName, parameterName, source);
                throw new BadHttpRequestException(message);
            }

            RequiredParameterNotProvided(GetLogger(httpContext), parameterTypeName, parameterName, source);
        }

        [LoggerMessage(4, LogLevel.Debug, RequiredParameterNotProvidedLogMessage, EventName = "RequiredParameterNotProvided")]
        private static partial void RequiredParameterNotProvided(ILogger logger, string parameterType, string parameterName, string source);

        public static void ImplicitBodyNotProvided(HttpContext httpContext, string parameterName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, ImplicitBodyNotProvidedExceptionMessage, parameterName);
                throw new BadHttpRequestException(message);
            }

            ImplicitBodyNotProvided(GetLogger(httpContext), parameterName);
        }

        [LoggerMessage(5, LogLevel.Debug, ImplicitBodyNotProvidedLogMessage, EventName = "ImplicitBodyNotProvided")]
        private static partial void ImplicitBodyNotProvided(ILogger logger, string parameterName);

        public static void UnexpectedJsonContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, UnexpectedJsonContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedJsonContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(6, LogLevel.Debug, UnexpectedJsonContentTypeLogMessage, EventName = "UnexpectedContentType")]
        private static partial void UnexpectedJsonContentType(ILogger logger, string contentType);

        public static void UnexpectedNonFormContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, UnexpectedFormContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedNonFormContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(7, LogLevel.Debug, UnexpectedFormContentTypeLogMessage, EventName = "UnexpectedNonFormContentType")]
        private static partial void UnexpectedNonFormContentType(ILogger logger, string contentType);

        public static void InvalidFormRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, InvalidFormRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidFormRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(8, LogLevel.Debug, InvalidFormRequestBodyMessage, EventName = "InvalidFormRequestBody")]
        private static partial void InvalidFormRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

        private static ILogger GetLogger(HttpContext httpContext)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger(typeof(RequestDelegateFactory));
        }
    }
}
