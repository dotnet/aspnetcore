// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// The context used for <see cref="IHttpLoggingInterceptor"/>.
/// </summary>
/// <remarks>
/// Settings will be pre-initialized with the relevant values from <see cref="HttpLoggingOptions" /> and updated with endpoint specific
/// values from <see cref="HttpLoggingAttribute"/> or
/// <see cref="HttpLoggingEndpointConventionBuilderExtensions.WithHttpLogging{TBuilder}(TBuilder, HttpLoggingFields, int?, int?)" />.
/// All settings can be modified per request. All settings will carry over from
/// <see cref="IHttpLoggingInterceptor.OnRequestAsync(HttpLoggingInterceptorContext)"/>
/// to <see cref="IHttpLoggingInterceptor.OnResponseAsync(HttpLoggingInterceptorContext)"/> except the <see cref="Parameters"/>
/// which are cleared after logging the request.
/// </remarks>
public sealed class HttpLoggingInterceptorContext
{
    private HttpContext? _httpContext;

    /// <summary>
    /// The request context.
    /// </summary>
    /// <remarks>
    /// This property should not be set by user code except for testing purposes.
    /// </remarks>
    public HttpContext HttpContext
    {
        get => _httpContext ?? throw new InvalidOperationException("HttpContext was not initialized.");
        // Public for 3rd party testing of interceptors.
        // We'd make this a required constructor/init parameter but ObjectPool requires a parameterless constructor.
        set => _httpContext = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets which parts of the request and response to log.
    /// </summary>
    /// <remarks>
    /// This is pre-populated with the value from <see cref="HttpLoggingOptions.LoggingFields"/>,
    /// <see cref="HttpLoggingAttribute.LoggingFields"/>, or
    /// <see cref="HttpLoggingEndpointConventionBuilderExtensions.WithHttpLogging{TBuilder}(TBuilder, HttpLoggingFields, int?, int?)"/>.
    /// </remarks>
    public HttpLoggingFields LoggingFields { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of bytes of the request body to log.
    /// </summary>
    /// <remarks>
    /// This is pre-populated with the value from <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>,
    /// <see cref="HttpLoggingAttribute.RequestBodyLogLimit"/>, or
    /// <see cref="HttpLoggingEndpointConventionBuilderExtensions.WithHttpLogging{TBuilder}(TBuilder, HttpLoggingFields, int?, int?)"/>.
    /// </remarks>
    public int RequestBodyLogLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of bytes of the response body to log.
    /// </summary>
    /// <remarks>
    /// This is pre-populated with the value from <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>,
    /// <see cref="HttpLoggingAttribute.ResponseBodyLogLimit"/>, or
    /// <see cref="HttpLoggingEndpointConventionBuilderExtensions.WithHttpLogging{TBuilder}(TBuilder, HttpLoggingFields, int?, int?)"/>.
    /// </remarks>
    public int ResponseBodyLogLimit { get; set; }

    internal long StartTimestamp { get; set; }
    internal TimeProvider TimeProvider { get; set; } = null!;
    internal List<KeyValuePair<string, object?>> InternalParameters { get; set; } = new();

    /// <summary>
    /// Gets a list of parameters that will be logged as part of the request or response. Values specified in <see cref="LoggingFields"/>
    /// will be added automatically after all interceptors run. All values are cleared after logging the request.
    /// All other relevant settings will carry over to the response.
    /// </summary>
    /// <remarks>
    /// If <see cref="HttpLoggingOptions.CombineLogs"/> is enabled, the parameters will be logged as part of the combined log.
    /// </remarks>
    public IList<KeyValuePair<string, object?>> Parameters => InternalParameters;

    /// <summary>
    /// Adds data that will be logged as part of the request or response. See <see cref="Parameters"/>.
    /// </summary>
    /// <param name="key">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    public void AddParameter(string key, object? value)
    {
        InternalParameters.Add(new(key, value));
    }

    /// <summary>
    /// Adds the given fields to what's currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields">Additional fields to enable.</param>
    public void Enable(HttpLoggingFields fields)
    {
        LoggingFields |= fields;
    }

    /// <summary>
    /// Checks if any of the given fields are currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields">One or more field flags to check.</param>
    public bool IsAnyEnabled(HttpLoggingFields fields)
    {
        return (LoggingFields & fields) != HttpLoggingFields.None;
    }

    /// <summary>
    /// Removes the given fields from what's currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields">Fields to disable.</param>
    public void Disable(HttpLoggingFields fields)
    {
        LoggingFields &= ~fields;
    }

    /// <summary>
    /// Disables the given fields if any are currently enabled in <see cref="LoggingFields"/>.
    /// </summary>
    /// <param name="fields">One or more field flags to disable if present.</param>
    /// <returns><see langword="true" /> if any of the fields were previously enabled.</returns>
    public bool TryDisable(HttpLoggingFields fields)
    {
        if (IsAnyEnabled(fields))
        {
            Disable(fields);
            return true;
        }

        return false;
    }

    internal void Reset()
    {
        _httpContext = null;
        LoggingFields = HttpLoggingFields.None;
        RequestBodyLogLimit = 0;
        ResponseBodyLogLimit = 0;
        StartTimestamp = 0;
        TimeProvider = null!;
        // Not pooled, the logger may store a reference to this instance.
        InternalParameters = new();
    }

    internal double GetDuration()
    {
        return TimeProvider.GetElapsedTime(StartTimestamp).TotalMilliseconds;
    }
}
