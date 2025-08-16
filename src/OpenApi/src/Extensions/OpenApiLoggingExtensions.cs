// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OpenApi;

internal static partial class OpenApiLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Failed to apply default value for parameter due to type mismatch. Default value type: '{DefaultValueType}', Parameter type: '{ParameterType}'. Default value will be omitted from the OpenAPI schema.", EventName = "DefaultValueTypeMismatch")]
    public static partial void DefaultValueTypeMismatch(this ILogger logger, string defaultValueType, string parameterType);
}