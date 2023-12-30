// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution invokes <see cref="M:HttpContext.SignOutAsync"/>.
/// </summary>
public sealed partial class SignOutHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SignOutHttpResult"/> with the default sign out scheme.
    /// </summary>
    internal SignOutHttpResult()
        : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SignOutHttpResult"/> with the
    /// specified authentication schemes.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication schemes to use when signing out the user.</param>
    internal SignOutHttpResult(IList<string> authenticationSchemes)
        : this(authenticationSchemes, properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SignOutHttpResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationScheme">The authentication schemes to use when signing out the user.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
    internal SignOutHttpResult(string authenticationScheme, AuthenticationProperties? properties)
        : this(new[] { authenticationScheme }, properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SignOutHttpResult"/> with the
    /// specified authentication schemes and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication scheme to use when signing out the user.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
    internal SignOutHttpResult(IList<string> authenticationSchemes, AuthenticationProperties? properties)
    {
        ArgumentNullException.ThrowIfNull(authenticationSchemes);

        AuthenticationSchemes = authenticationSchemes.AsReadOnly();
        Properties = properties;
    }

    /// <summary>
    /// Gets the authentication schemes that are challenged.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; internal init; }

    /// <summary>
    /// Gets the <see cref="AuthenticationProperties"/> used to perform the sign-out operation.
    /// </summary>
    public AuthenticationProperties? Properties { get; internal init; }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.SignOutResult");

        Log.SignOutResultExecuting(logger, AuthenticationSchemes);

        if (AuthenticationSchemes.Count == 0)
        {
            await httpContext.SignOutAsync(Properties);
        }
        else
        {
            for (var i = 0; i < AuthenticationSchemes.Count; i++)
            {
                await httpContext.SignOutAsync(AuthenticationSchemes[i], Properties);
            }
        }
    }

    private static partial class Log
    {
        public static void SignOutResultExecuting(ILogger logger, IReadOnlyList<string> authenticationSchemes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                SignOutResultExecuting(logger, authenticationSchemes.ToArray());
            }
        }

        [LoggerMessage(1, LogLevel.Information,
            "Executing SignOutResult with authentication schemes ({Schemes}).",
            EventName = "SignOutResultExecuting",
            SkipEnabledCheck = true)]
        private static partial void SignOutResultExecuting(ILogger logger, string[] schemes);
    }
}
