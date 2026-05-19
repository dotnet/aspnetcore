// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Callbacks used to extend the HttpLogging middleware.
/// </summary>
public interface IHttpLoggingInterceptor
{
    /// <summary>
    /// A callback to customize the logging of the request and response.
    /// </summary>
    /// <remarks>
    /// This is called when the request is first received and can be used to configure both request and response options. All settings will carry over to
    /// <see cref="OnResponseAsync(HttpLoggingInterceptorContext)"/> except the <see cref="HttpLoggingInterceptorContext.Parameters"/>
    /// will be cleared after logging the request. <see cref="HttpLoggingInterceptorContext.LoggingFields"/> may be changed per request to control the logging behavior.
    /// If no request fields are enabled, and the <see cref="HttpLoggingInterceptorContext.Parameters"/> collection is empty, no request logging will occur.
    /// If <see cref="HttpLoggingOptions.CombineLogs"/> is enabled then <see cref="HttpLoggingInterceptorContext.Parameters"/> will carry over from the request to response
    /// and be logged together.
    /// </remarks>
    ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext);

    /// <summary>
    /// A callback to customize the logging of the response.
    /// </summary>
    /// <remarks>
    /// This is called when the first write to the response happens, or the response ends without a write, just before anything is sent to the client. Settings are carried
    /// over from <see cref="OnRequestAsync(HttpLoggingInterceptorContext)"/> (except the <see cref="HttpLoggingInterceptorContext.Parameters"/>) and response settings may
    /// still be modified. Changes to request settings will have no effect. If no response fields are enabled, and the <see cref="HttpLoggingInterceptorContext.Parameters"/>
    /// collection is empty, no response logging will occur.
    /// If <see cref="HttpLoggingOptions.CombineLogs"/> is enabled then <see cref="HttpLoggingInterceptorContext.Parameters"/> will carry over from the request to response
    /// and be logged together. <see cref="HttpLoggingFields.RequestBody"/> and <see cref="HttpLoggingFields.ResponseBody"/>  can also be disabled in OnResponseAsync to prevent
    /// logging any buffered body data.
    /// </remarks>
    ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext);
}
