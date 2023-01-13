// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
// Additional values are specified on JsonSerializerContext to support some values for extensions.
// For example, the DeveloperExceptionMiddleware serializes its complex type to JsonElement, which problem details then needs to serialize.
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
{
}
