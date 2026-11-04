// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Options for the Twitter authentication handler.
/// </summary>
public class TwitterOptions : RemoteAuthenticationOptions
{
    private const string DefaultStateCookieName = "__TwitterState";

    private CookieBuilder _stateCookieBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterOptions"/> class.
    /// </summary>
    public TwitterOptions()
    {
        CallbackPath = new PathString("/signin-twitter");
        BackchannelTimeout = TimeSpan.FromSeconds(60);
        Events = new TwitterEvents();

        ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);

        _stateCookieBuilder = new TwitterCookieBuilder(this)
        {
            Name = DefaultStateCookieName,
            SecurePolicy = CookieSecurePolicy.SameAsRequest,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        };
    }

    /// <summary>
    /// Gets or sets the consumer key used to communicate with Twitter.
    /// </summary>
    /// <value>The consumer key used to communicate with Twitter.</value>
    public string? ConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets the consumer secret used to sign requests to Twitter.
    /// </summary>
    /// <value>The consumer secret used to sign requests to Twitter.</value>
    public string? ConsumerSecret { get; set; }

    /// <summary>
    /// Enables the retrieval user details during the authentication process, including
    /// e-mail addresses. Retrieving e-mail addresses requires special permissions
    /// from Twitter Support on a per application basis. The default is false.
    /// See <see href="https://dev.twitter.com/rest/reference/get/account/verify_credentials"/>.
    /// </summary>
    public bool RetrieveUserDetails { get; set; }

    /// <summary>
    /// A collection of claim actions used to select values from the json user data and create Claims.
    /// </summary>
    public ClaimActionCollection ClaimActions { get; } = new ClaimActionCollection();

    /// <summary>
    /// Gets or sets the type used to secure data handled by the handler.
    /// </summary>
    public ISecureDataFormat<RequestToken> StateDataFormat { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="TwitterEvents"/> used to handle authentication events.
    /// </summary>
    public new TwitterEvents Events
    {
        get => (TwitterEvents)base.Events;
        set => base.Events = value;
    }

    /// <summary>
    /// Determines the settings used to create the state cookie before the
    /// cookie gets added to the response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If an explicit <see cref="CookieBuilder.Name"/> is not provided, the system will automatically generate a
    /// unique name that begins with <c>__TwitterState</c>.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Lax"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.IsEssential"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.SameAsRequest"/>.</description></item>
    /// </list>
    /// </remarks>
    public CookieBuilder StateCookie
    {
        get => _stateCookieBuilder;
        set => _stateCookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Added the validate method to ensure that the customer key and customer secret values are not not empty for the twitter authentication middleware
    /// </summary>
    public override void Validate()
    {
        base.Validate();
        ArgumentException.ThrowIfNullOrEmpty(ConsumerKey);
        ArgumentException.ThrowIfNullOrEmpty(ConsumerSecret);
    }

    private sealed class TwitterCookieBuilder : CookieBuilder
    {
        private readonly TwitterOptions _twitterOptions;

        public TwitterCookieBuilder(TwitterOptions twitterOptions)
        {
            _twitterOptions = twitterOptions;
        }

        public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
        {
            var options = base.Build(context, expiresFrom);
            if (!Expiration.HasValue)
            {
                options.Expires = expiresFrom.Add(_twitterOptions.RemoteAuthenticationTimeout);
            }
            return options;
        }
    }
}
