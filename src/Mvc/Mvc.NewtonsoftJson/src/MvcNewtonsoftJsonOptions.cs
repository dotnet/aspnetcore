// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides programmatic configuration for JSON formatters using Newtonsoft.JSON.
/// </summary>
public class MvcNewtonsoftJsonOptions : IEnumerable<ICompatibilitySwitch>
{
    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();

    /// <summary>
    /// Gets or sets a flag to determine whether error messages from JSON deserialization by the
    /// <see cref="NewtonsoftJsonInputFormatter"/> will be added to the <see cref="ModelStateDictionary"/>. If
    /// <see langword="false"/>, a generic error message will be used instead.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// Error messages in the <see cref="ModelStateDictionary"/> are often communicated to clients, either in HTML
    /// or using <see cref="BadRequestObjectResult"/>. In effect, this setting controls whether clients can receive
    /// detailed error messages about submitted JSON data.
    /// </remarks>
    public bool AllowInputFormatterExceptionMessages { get; set; } = true;

    /// <summary>
    /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
    /// </summary>
    public JsonSerializerSettings SerializerSettings { get; } = JsonSerializerSettingsProvider.CreateSerializerSettings();

    /// <summary>
    /// Gets the maximum size to buffer in memory when <see cref="MvcOptions.SuppressInputFormatterBuffering"/> is not set.
    /// <para>
    /// <see cref="NewtonsoftJsonInputFormatter"/> buffers the input stream by default, buffering up to a certain amount in memory, before buffering to disk.
    /// This option configures the size in bytes that MVC will buffer in memory, before switching to disk.
    /// </para>
    /// </summary>
    /// <value>Defaults to 30Kb.</value>
    public int InputFormatterMemoryBufferThreshold { get; set; } = 1024 * 30;

    /// <summary>
    /// Gets or sets a flag to determine whether the value of <see cref="CultureInfo.CurrentCulture"/>
    /// for the current HTTP request is used for JSON deserialization by <see cref="NewtonsoftJsonInputFormatter"/>.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool ReadJsonWithRequestCulture { get; set; }

    /// <summary>
    /// Gets the maximum size to buffer in memory when <see cref="MvcOptions.SuppressOutputFormatterBuffering"/> is not set.
    /// <para>
    /// <see cref="NewtonsoftJsonOutputFormatter"/> buffers the output stream by default, buffering up to a certain amount in memory, before buffering to disk.
    /// This option configures the size in bytes that MVC will buffer in memory, before switching to disk.
    /// </para>
    /// </summary>
    /// <value>Defaults to 30Kb.</value>
    public int OutputFormatterMemoryBufferThreshold { get; set; } = 1024 * 30;

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
