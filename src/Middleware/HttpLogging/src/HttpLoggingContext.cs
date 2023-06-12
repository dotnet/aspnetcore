// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// The context used for logging customization callbacks.
/// </summary>
/// <param name="httpContext">The request context.</param>
public sealed class HttpLoggingContext(HttpContext httpContext)
{
    /// <summary>
    /// The request context.
    /// </summary>
    public HttpContext HttpContext { get; internal set; } = httpContext;

    /// <summary>
    /// What parts of the request and response to log.
    /// </summary>
    public HttpLoggingFields LoggingFields { get; set; }

    /// <summary>
    /// Limits how much of the request body to log.
    /// </summary>
    public int RequestBodyLogLimit { get; set; }

    /// <summary>
    /// Limits how much of the response body to log.
    /// </summary>
    public int ResponseBodyLogLimit { get; set; }

    internal long StartTimestamp { get; set; }

    /// <summary>
    /// The parameters to log.
    /// </summary>
    public IList<KeyValuePair<string, object?>> Parameters { get; } = new List<KeyValuePair<string, object?>>();

    /// <summary>
    /// Adds a parameter to the log context.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    public void Add(string key, object? value)
    {
        Parameters.Add(new(key, value));
    }

    /// <summary>
    /// Adds the given fields to what's currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields"></param>
    public void Enable(HttpLoggingFields fields)
    {
        LoggingFields |= fields;
    }

    /// <summary>
    /// Checks if any of the given fields are currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    public bool IsAnyEnabled(HttpLoggingFields fields)
    {
        return (LoggingFields & fields) != HttpLoggingFields.None;
    }

    /// <summary>
    /// Removes the given fields from what's currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields"></param>
    public void Disable(HttpLoggingFields fields)
    {
        LoggingFields &= ~fields;
    }

    /// <summary>
    /// Checks if the given field is currently enabled in <see cref="LoggingFields"/>
    /// and disables it so that a custom log value can be provided instead.
    /// </summary>
    /// <param name="field">A single field flag to check.</param>
    /// <returns>`true` if the field was enabled.</returns>
    public bool TryOverride(HttpLoggingFields field)
    {
        if (LoggingFields.HasFlag(field))
        {
            Disable(field);
            return true;
        }

        return false;
    }
}
