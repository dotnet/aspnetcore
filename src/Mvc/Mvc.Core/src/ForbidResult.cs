// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that on execution invokes <see cref="M:HttpContext.ForbidAsync"/>.
/// </summary>
public partial class ForbidResult : ActionResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/>.
    /// </summary>
    public ForbidResult()
        : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/> with the
    /// specified authentication scheme.
    /// </summary>
    /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
    public ForbidResult(string authenticationScheme)
        : this(new[] { authenticationScheme })
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/> with the
    /// specified authentication schemes.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    public ForbidResult(IList<string> authenticationSchemes)
        : this(authenticationSchemes, properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/> with the
    /// specified <paramref name="properties"/>.
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ForbidResult(AuthenticationProperties? properties)
        : this(Array.Empty<string>(), properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ForbidResult(string authenticationScheme, AuthenticationProperties? properties)
        : this(new[] { authenticationScheme }, properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidResult"/> with the
    /// specified authentication schemes and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ForbidResult(IList<string> authenticationSchemes, AuthenticationProperties? properties)
    {
        AuthenticationSchemes = authenticationSchemes;
        Properties = properties;
    }

    /// <summary>
    /// Gets or sets the authentication schemes that are challenged.
    /// </summary>
    public IList<string> AuthenticationSchemes { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AuthenticationProperties"/> used to perform the authentication challenge.
    /// </summary>
    public AuthenticationProperties? Properties { get; set; }

    /// <inheritdoc />
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(ForbidResult));
        Log.ForbidResultExecuting(logger, AuthenticationSchemes);

        if (AuthenticationSchemes != null && AuthenticationSchemes.Count > 0)
        {
            for (var i = 0; i < AuthenticationSchemes.Count; i++)
            {
                await httpContext.ForbidAsync(AuthenticationSchemes[i], Properties);
            }
        }
        else
        {
            await httpContext.ForbidAsync(Properties);
        }
    }

    private static partial class Log
    {
        public static void ForbidResultExecuting(ILogger logger, IList<string> authenticationSchemes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ForbidResultExecuting(logger, authenticationSchemes.ToArray());
            }
        }

        [LoggerMessage(1, LogLevel.Information, $"Executing {nameof(ForbidResult)} with authentication schemes ({{Schemes}}).", EventName = "ForbidResultExecuting", SkipEnabledCheck = true)]
        private static partial void ForbidResultExecuting(ILogger logger, string[] schemes);
    }
}
