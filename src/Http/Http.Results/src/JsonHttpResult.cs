// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
internal sealed partial class JsonHttpResult : ObjectHttpResult
{
    public JsonHttpResult(object? value, JsonSerializerOptions? jsonSerializerOptions)
        : base(value)
    {
        JsonSerializerOptions = jsonSerializerOptions;
    }

    public JsonHttpResult(object? value, int? statusCode, JsonSerializerOptions? jsonSerializerOptions)
        : base(value, statusCode)
    {
        JsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// <para>
    /// When using <c>System.Text.Json</c>, this should be an instance of <see cref="JsonSerializerOptions" />
    /// </para>
    /// <para>
    /// When using <c>Newtonsoft.Json</c>, this should be an instance of <c>JsonSerializerSettings</c>.
    /// </para>
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    protected internal override Task WriteHttpResultAsync(HttpContext httpContext)
        => httpContext.Response.WriteAsJsonAsync(Value, JsonSerializerOptions, ContentType);
}
