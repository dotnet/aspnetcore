// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that on execution invokes <see cref="M:HttpContext.ChallengeAsync"/>.
/// </summary>
public partial class ChallengeResult : ActionResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/>.
    /// </summary>
    public ChallengeResult()
        : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/> with the
    /// specified authentication scheme.
    /// </summary>
    /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
    public ChallengeResult(string authenticationScheme)
        : this(new[] { authenticationScheme })
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/> with the
    /// specified authentication schemes.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    public ChallengeResult(IList<string> authenticationSchemes)
        : this(authenticationSchemes, properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/> with the
    /// specified <paramref name="properties"/>.
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeResult(AuthenticationProperties? properties)
        : this(Array.Empty<string>(), properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeResult(string authenticationScheme, AuthenticationProperties? properties)
        : this(new[] { authenticationScheme }, properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeResult"/> with the
    /// specified authentication schemes and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeResult(IList<string> authenticationSchemes, AuthenticationProperties? properties)
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
        var logger = loggerFactory.CreateLogger(typeof(ChallengeResult));
        Log.ChallengeResultExecuting(logger, AuthenticationSchemes);

        if (AuthenticationSchemes != null && AuthenticationSchemes.Count > 0)
        {
            foreach (var scheme in AuthenticationSchemes)
            {
                await httpContext.ChallengeAsync(scheme, Properties);
            }
        }
        else
        {
            await httpContext.ChallengeAsync(Properties);
        }
    }

    private static partial class Log
    {
        public static void ChallengeResultExecuting(ILogger logger, IList<string> schemes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ChallengeResultExecuting(logger, schemes.ToArray());
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Executing ChallengeResult with authentication schemes ({Schemes}).", EventName = "ChallengeResultExecuting", SkipEnabledCheck = true)]
        private static partial void ChallengeResultExecuting(ILogger logger, string[] schemes);
    }
}
