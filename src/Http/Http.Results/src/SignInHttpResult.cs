// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution invokes <see cref="M:HttpContext.SignInAsync"/>.
/// </summary>
public sealed partial class SignInHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SignInHttpResult"/> with the
    /// default authentication scheme.
    /// </summary>
    /// <param name="principal">The claims principal containing the user claims.</param>
    internal SignInHttpResult(ClaimsPrincipal principal)
        : this(principal, authenticationScheme: null, properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SignInHttpResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="principal">The claims principal containing the user claims.</param>
    /// <param name="authenticationScheme">The authentication schemes to use when signing in the user.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
    internal SignInHttpResult(ClaimsPrincipal principal, string? authenticationScheme, AuthenticationProperties? properties)
    {
        Principal = principal;
        AuthenticationScheme = authenticationScheme;
        Properties = properties;
    }

    /// <summary>
    /// Gets or sets the authentication scheme that is used to perform the sign-in operation.
    /// </summary>
    public string? AuthenticationScheme { get; internal init; }

    /// <summary>
    /// Gets or sets the <see cref="ClaimsPrincipal"/> containing the user claims.
    /// </summary>
    public ClaimsPrincipal Principal { get; internal init; }

    /// <summary>
    /// Gets or sets the <see cref="AuthenticationProperties"/> used to perform the sign-in operation.
    /// </summary>
    public AuthenticationProperties? Properties { get; internal init; }

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.SignInResult");

        Log.SignInResultExecuting(logger, AuthenticationScheme, Principal);

        return httpContext.SignInAsync(AuthenticationScheme, Principal, Properties);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information,
            "Executing SignInResult with authentication scheme ({Scheme}) and the following principal: {Principal}.",
            EventName = "SignInResultExecuting")]
        public static partial void SignInResultExecuting(ILogger logger, string? scheme, ClaimsPrincipal principal);
    }
}
