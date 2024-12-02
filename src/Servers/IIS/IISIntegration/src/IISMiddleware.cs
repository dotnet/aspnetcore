// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration;

/// <summary>
/// The middleware that enables IIS Out-Of-Process to work.
/// </summary>
public class IISMiddleware
{
    private const string MSAspNetCoreClientCert = "MS-ASPNETCORE-CLIENTCERT";
    private const string MSAspNetCoreToken = "MS-ASPNETCORE-TOKEN";
    private const string MSAspNetCoreEvent = "MS-ASPNETCORE-EVENT";
    private const string MSAspNetCoreWinAuthToken = "MS-ASPNETCORE-WINAUTHTOKEN";
    private const string ANCMShutdownEventHeaderValue = "shutdown";
    private static readonly PathString ANCMRequestPath = new PathString("/iisintegration");
    private static readonly Func<object, Task> ClearUserDelegate = ClearUser;

    private readonly RequestDelegate _next;
    private readonly IISOptions _options;
    private readonly ILogger _logger;
    private readonly string _pairingToken;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly bool _isWebsocketsSupported;

    /// <summary>
    /// The middleware that enables IIS Out-Of-Process to work.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory" />.</param>
    /// <param name="options">The configuration for this middleware.</param>
    /// <param name="pairingToken">A token used to coordinate with the ASP.NET Core Module.</param>
    /// <param name="authentication">The <see cref="IAuthenticationSchemeProvider"/>.</param>
    /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
    // Can't break public API, so creating a second constructor to propagate the isWebsocketsSupported flag.
    public IISMiddleware(RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<IISOptions> options,
        string pairingToken,
        IAuthenticationSchemeProvider authentication,
        IHostApplicationLifetime applicationLifetime)
        : this(next, loggerFactory, options, pairingToken, isWebsocketsSupported: true, authentication, applicationLifetime)
    {
    }

    /// <summary>
    /// The middleware that enables IIS Out-Of-Process to work.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory" />.</param>
    /// <param name="options">The configuration for this middleware.</param>
    /// <param name="pairingToken">A token used to coordinate with the ASP.NET Core Module.</param>
    /// <param name="isWebsocketsSupported">Whether websockets are supported by IIS.</param>
    /// <param name="authentication">The <see cref="IAuthenticationSchemeProvider"/>.</param>
    /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/>.</param>
    public IISMiddleware(RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<IISOptions> options,
        string pairingToken,
        bool isWebsocketsSupported,
        IAuthenticationSchemeProvider authentication,
        IHostApplicationLifetime applicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        ArgumentException.ThrowIfNullOrEmpty(pairingToken);

        _next = next;
        _options = options.Value;

        if (_options.ForwardWindowsAuthentication)
        {
            authentication.AddScheme(new AuthenticationScheme(IISDefaults.AuthenticationScheme, _options.AuthenticationDisplayName, typeof(AuthenticationHandler)));
        }

        _pairingToken = pairingToken;
        _applicationLifetime = applicationLifetime;
        _logger = loggerFactory.CreateLogger<IISMiddleware>();
        _isWebsocketsSupported = isWebsocketsSupported;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task Invoke(HttpContext httpContext)
    {
        if (!string.Equals(_pairingToken, httpContext.Request.Headers[MSAspNetCoreToken], StringComparison.Ordinal))
        {
            _logger.LogError($"'{MSAspNetCoreToken}' does not match the expected pairing token '{_pairingToken}', request rejected.");
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.CompletedTask;
        }

        // Handle shutdown from ANCM
        if (HttpMethods.IsPost(httpContext.Request.Method) &&
            httpContext.Request.Path.Equals(ANCMRequestPath) &&
            string.Equals(ANCMShutdownEventHeaderValue, httpContext.Request.Headers[MSAspNetCoreEvent], StringComparison.OrdinalIgnoreCase))
        {
            // Execute shutdown task on background thread without waiting for completion
            var shutdownTask = Task.Run(_applicationLifetime.StopApplication);
            httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.CompletedTask;
        }

        if (Debugger.IsAttached && string.Equals("DEBUG", httpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
        {
            // The Visual Studio debugger tooling sends a DEBUG request to make IIS & AspNetCoreModule launch the process
            // so the debugger can attach. Filter out this request from the app.
            return Task.CompletedTask;
        }

        var bodySizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature != null && !bodySizeFeature.IsReadOnly)
        {
            // IIS already limits this, no need to do it twice.
            bodySizeFeature.MaxRequestBodySize = null;
        }

        if (_options.ForwardClientCertificate)
        {
            var header = httpContext.Request.Headers[MSAspNetCoreClientCert];
            if (!StringValues.IsNullOrEmpty(header))
            {
                httpContext.Features.Set<ITlsConnectionFeature>(new ForwardedTlsConnectionFeature(_logger, header));
            }
        }

        if (_options.ForwardWindowsAuthentication)
        {
            // We must always process and clean up the windows identity, even if we don't assign the User.
            var user = GetUser(httpContext);
            if (user != null)
            {
                // Flow it through to the authentication handler.
                httpContext.Features.Set(user);

                if (_options.AutomaticAuthentication)
                {
                    httpContext.User = user;
                }
            }
        }

        // Remove the upgrade feature if websockets are not supported by ANCM.
        // The feature must be removed on a per request basis as the Upgrade feature exists per request.
        if (!_isWebsocketsSupported)
        {
            httpContext.Features.Set<IHttpUpgradeFeature?>(null);
        }

        return _next(httpContext);
    }

    private static WindowsPrincipal? GetUser(HttpContext context)
    {
        var tokenHeader = context.Request.Headers[MSAspNetCoreWinAuthToken];

        if (!StringValues.IsNullOrEmpty(tokenHeader)
            && int.TryParse(tokenHeader, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexHandle))
        {
            // Always create the identity if the handle exists, we need to dispose it so it does not leak.
            var handle = new IntPtr(hexHandle);
            var winIdentity = new WindowsIdentity(handle, IISDefaults.AuthenticationScheme);

            // WindowsIdentity just duplicated the handle so we need to close the original.
            NativeMethods.CloseHandle(handle);

            context.Response.OnCompleted(ClearUserDelegate, context);
            context.Response.RegisterForDispose(winIdentity);
            return new WindowsPrincipal(winIdentity);
        }

        return null;
    }

    private static Task ClearUser(object arg)
    {
        var context = (HttpContext)arg;
        // We don't want loggers accessing a disposed identity.
        // https://github.com/aspnet/Logging/issues/543#issuecomment-321907828
        if (context.User is WindowsPrincipal)
        {
            context.User = null!;
        }
        return Task.CompletedTask;
    }
}
