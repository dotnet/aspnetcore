// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution invokes <see cref="M:HttpContext.ForbidAsync"/>.
/// </summary>
public sealed partial class ForbidHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForbidHttpResult"/>.
    /// </summary>
    internal ForbidHttpResult()
        : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidHttpResult"/> with the
    /// specified authentication schemes.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    internal ForbidHttpResult(IList<string> authenticationSchemes)
        : this(authenticationSchemes, properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidHttpResult"/> with the
    /// specified <paramref name="properties"/>.
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    internal ForbidHttpResult(AuthenticationProperties? properties)
        : this(Array.Empty<string>(), properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidHttpResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationScheme">The authentication schemes to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    internal ForbidHttpResult(string authenticationScheme, AuthenticationProperties? properties)
        : this(new[] { authenticationScheme }, properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbidHttpResult"/> with the
    /// specified authentication schemes and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication scheme to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    internal ForbidHttpResult(IList<string> authenticationSchemes, AuthenticationProperties? properties)
    {
        AuthenticationSchemes = authenticationSchemes.AsReadOnly();
        Properties = properties;
    }

    /// <summary>
    /// Gets the authentication schemes that are challenged.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; internal init; }

    /// <summary>
    /// Gets the <see cref="AuthenticationProperties"/> used to perform the authentication challenge.
    /// </summary>
    public AuthenticationProperties? Properties { get; internal init; }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.ForbidResult");

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
        public static void ForbidResultExecuting(ILogger logger, IReadOnlyList<string> authenticationSchemes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ForbidResultExecuting(logger, authenticationSchemes.ToArray());
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Executing ChallengeResult with authentication schemes ({Schemes}).", EventName = "ChallengeResultExecuting", SkipEnabledCheck = true)]
        private static partial void ForbidResultExecuting(ILogger logger, string[] schemes);
    }
}
