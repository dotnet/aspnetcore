// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static class RequestDelegateCreationLogging
{
    public const int RequestBodyIOExceptionEventId = 1;
    public const string RequestBodyIOExceptionMessage = "Reading the request body failed with an IOException.";

    public const int InvalidJsonRequestBodyEventId = 2;
    public const string InvalidJsonRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as JSON.";
    public const string InvalidJsonRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as JSON.";

    public const int ParameterBindingFailedEventId = 3;
    public const string ParameterBindingFailedLogMessage = @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".";
    public const string ParameterBindingFailedExceptionMessage = @"Failed to bind parameter ""{0} {1}"" from ""{2}"".";

    public const int RequiredParameterNotProvidedEventId = 4;
    public const string RequiredParameterNotProvidedLogMessage = @"Required parameter ""{ParameterType} {ParameterName}"" was not provided from {Source}.";
    public const string RequiredParameterNotProvidedExceptionMessage = @"Required parameter ""{0} {1}"" was not provided from {2}.";

    public const int ImplicitBodyNotProvidedEventId = 5;
    public const string ImplicitBodyNotProvidedLogMessage = @"Implicit body inferred for parameter ""{ParameterName}"" but no body was provided. Did you mean to use a Service instead?";
    public const string ImplicitBodyNotProvidedExceptionMessage = @"Implicit body inferred for parameter ""{0}"" but no body was provided. Did you mean to use a Service instead?";

    public const int UnexpectedJsonContentTypeEventId = 6;
    public const string UnexpectedJsonContentTypeLogMessage = @"Expected a supported JSON media type but got ""{ContentType}"".";
    public const string UnexpectedJsonContentTypeExceptionMessage = @"Expected a supported JSON media type but got ""{0}"".";

    public const int UnexpectedFormContentTypeLogEventId = 7;
    public const string UnexpectedFormContentTypeLogMessage = @"Expected a supported form media type but got ""{ContentType}"".";
    public const string UnexpectedFormContentTypeExceptionMessage = @"Expected a supported form media type but got ""{0}"".";

    public const int InvalidFormRequestBodyEventId = 8;
    public const string InvalidFormRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as form.";
    public const string InvalidFormRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as form.";
}
