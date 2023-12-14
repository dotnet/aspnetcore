// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
public class JsonResult : ActionResult, IStatusCodeActionResult
{
    /// <summary>
    /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to format as JSON.</param>
    public JsonResult(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to format as JSON.</param>
    /// <param name="serializerSettings">
    /// The serializer settings to be used by the formatter.
    /// <para>
    /// When using <c>System.Text.Json</c>, this should be an instance of <see cref="JsonSerializerOptions" />.
    /// </para>
    /// <para>
    /// When using <c>Newtonsoft.Json</c>, this should be an instance of <c>JsonSerializerSettings</c>.
    /// </para>
    /// </param>
    public JsonResult(object? value, object? serializerSettings)
    {
        Value = value;
        SerializerSettings = serializerSettings;
    }

    /// <summary>
    /// Gets or sets the <see cref="Net.Http.Headers.MediaTypeHeaderValue"/> representing the Content-Type header of the response.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// <para>
    /// When using <c>System.Text.Json</c>, this should be an instance of <see cref="JsonSerializerOptions" />
    /// </para>
    /// <para>
    /// When using <c>Newtonsoft.Json</c>, this should be an instance of <c>JsonSerializerSettings</c>.
    /// </para>
    /// </summary>
    public object? SerializerSettings { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the value to be formatted.
    /// </summary>
    public object? Value { get; set; }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.HttpContext.RequestServices;
        var executor = services.GetRequiredService<IActionResultExecutor<JsonResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
