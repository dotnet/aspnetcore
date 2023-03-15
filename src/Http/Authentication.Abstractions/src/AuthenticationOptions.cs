// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Options to configure authentication.
/// </summary>
public class AuthenticationOptions
{
    private readonly IList<AuthenticationSchemeBuilder> _schemes = new List<AuthenticationSchemeBuilder>();

    /// <summary>
    /// Returns the schemes in the order they were added (important for request handling priority)
    /// </summary>
    public IEnumerable<AuthenticationSchemeBuilder> Schemes => _schemes;

    /// <summary>
    /// Maps schemes by name.
    /// </summary>
    public IDictionary<string, AuthenticationSchemeBuilder> SchemeMap { get; } = new Dictionary<string, AuthenticationSchemeBuilder>(StringComparer.Ordinal);

    /// <summary>
    /// Adds an <see cref="AuthenticationScheme"/>.
    /// </summary>
    /// <param name="name">The name of the scheme being added.</param>
    /// <param name="configureBuilder">Configures the scheme.</param>
    public void AddScheme(string name, Action<AuthenticationSchemeBuilder> configureBuilder)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureBuilder);

        if (SchemeMap.ContainsKey(name))
        {
            throw new InvalidOperationException("Scheme already exists: " + name);
        }

        var builder = new AuthenticationSchemeBuilder(name);
        configureBuilder(builder);
        _schemes.Add(builder);
        SchemeMap[name] = builder;
    }

    /// <summary>
    /// Adds an <see cref="AuthenticationScheme"/>.
    /// </summary>
    /// <typeparam name="THandler">The <see cref="IAuthenticationHandler"/> responsible for the scheme.</typeparam>
    /// <param name="name">The name of the scheme being added.</param>
    /// <param name="displayName">The display name for the scheme.</param>
    public void AddScheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string name, string? displayName) where THandler : IAuthenticationHandler
    {
        AddScheme(name, b =>
        {
            b.DisplayName = displayName;
            b.HandlerType = typeof(THandler);
        });
    }

    /// <summary>
    /// Used as the fallback default scheme for all the other defaults.
    /// </summary>
    public string? DefaultScheme { get; set; }

    /// <summary>
    /// Used as the default scheme by <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
    /// </summary>
    public string? DefaultAuthenticateScheme { get; set; }

    /// <summary>
    /// Used as the default scheme by <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
    /// </summary>
    public string? DefaultSignInScheme { get; set; }

    /// <summary>
    /// Used as the default scheme by <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.
    /// </summary>
    public string? DefaultSignOutScheme { get; set; }

    /// <summary>
    /// Used as the default scheme by <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
    /// </summary>
    public string? DefaultChallengeScheme { get; set; }

    /// <summary>
    /// Used as the default scheme by <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.
    /// </summary>
    public string? DefaultForbidScheme { get; set; }

    /// <summary>
    /// If true, SignIn should throw if attempted with a user is not authenticated.
    /// A user is considered authenticated if <see cref="ClaimsIdentity.IsAuthenticated"/> returns <see langword="true" /> for the <see cref="ClaimsPrincipal"/> associated with the HTTP request.
    /// </summary>
    public bool RequireAuthenticatedSignIn { get; set; } = true;

    /// <summary>
    /// If true, DefaultScheme will not automatically use a single registered scheme.
    /// </summary>
    private bool? _disableAutoDefaultScheme;
    internal bool DisableAutoDefaultScheme
    {
        get
        {
            if (!_disableAutoDefaultScheme.HasValue)
            {
                _disableAutoDefaultScheme = AppContext.TryGetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", out var enabled) && enabled;
            }

            return _disableAutoDefaultScheme.Value;
        }
        set => _disableAutoDefaultScheme = value;
    }
}
