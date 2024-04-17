// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection"/> for configuring identity services.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default identity system configuration for the specified User and Role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a User in the system.</typeparam>
    /// <typeparam name="TRole">The type representing a Role in the system.</typeparam>
    /// <param name="services">The services available in the application.</param>
    /// <returns>An <see cref="IdentityBuilder"/> for creating and configuring the identity system.</returns>
    [RequiresUnreferencedCode("Identity middleware does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IdentityBuilder AddIdentity<TUser, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRole>(
        this IServiceCollection services)
        where TUser : class
        where TRole : class
        => services.AddIdentity<TUser, TRole>(setupAction: null!);

    /// <summary>
    /// Adds and configures the identity system for the specified User and Role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a User in the system.</typeparam>
    /// <typeparam name="TRole">The type representing a Role in the system.</typeparam>
    /// <param name="services">The services available in the application.</param>
    /// <param name="setupAction">An action to configure the <see cref="IdentityOptions"/>.</param>
    /// <returns>An <see cref="IdentityBuilder"/> for creating and configuring the identity system.</returns>
    [RequiresUnreferencedCode("Identity middleware does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IdentityBuilder AddIdentity<TUser, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRole>(
        this IServiceCollection services,
        Action<IdentityOptions> setupAction)
        where TUser : class
        where TRole : class
    {
        // Services used by identity
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddCookie(IdentityConstants.ApplicationScheme, o =>
        {
            o.LoginPath = new PathString("/Account/Login");
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
            };
        })
        .AddCookie(IdentityConstants.ExternalScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.ExternalScheme;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
            };
        })
        .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
            o.Events = new CookieAuthenticationEvents
            {
                OnRedirectToReturnUrl = _ => Task.CompletedTask
            };
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        // Hosting doesn't add IHttpContextAccessor by default
        services.AddHttpContextAccessor();
        // Identity services
        services.TryAddScoped<IUserValidator<TUser>, UserValidator<TUser>>();
        services.TryAddScoped<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
        services.TryAddScoped<IPasswordHasher<TUser>, PasswordHasher<TUser>>();
        services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.TryAddScoped<IRoleValidator<TRole>, RoleValidator<TRole>>();
        // No interface for the error describer so we can add errors without rev'ing the interface
        services.TryAddScoped<IdentityErrorDescriber>();
        services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<TUser>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<SecurityStampValidatorOptions>, PostConfigureSecurityStampValidatorOptions>());
        services.TryAddScoped<ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<TUser>>();
        services.TryAddScoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser, TRole>>();
        services.TryAddScoped<IUserConfirmation<TUser>, DefaultUserConfirmation<TUser>>();
        services.TryAddScoped<UserManager<TUser>>();
        services.TryAddScoped<SignInManager<TUser>>();
        services.TryAddScoped<RoleManager<TRole>>();

        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        return new IdentityBuilder(typeof(TUser), typeof(TRole), services);
    }

    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens and cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityApiEndpoints<TUser>(this IServiceCollection services)
        where TUser : class, new()
        => services.AddIdentityApiEndpoints<TUser>(_ => { });

    /// <summary>
    /// Adds a set of common identity services to the application to support <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi{TUser}(IEndpointRouteBuilder)"/>
    /// and configures authentication to support identity bearer tokens and cookies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Configures the <see cref="IdentityOptions"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddIdentityApiEndpoints<TUser>(this IServiceCollection services, Action<IdentityOptions> configure)
        where TUser : class, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services
            .AddAuthentication(IdentityConstants.BearerAndApplicationScheme)
            .AddScheme<AuthenticationSchemeOptions, CompositeIdentityHandler>(IdentityConstants.BearerAndApplicationScheme, null, compositeOptions =>
            {
                compositeOptions.ForwardDefault = IdentityConstants.BearerScheme;
                compositeOptions.ForwardAuthenticate = IdentityConstants.BearerAndApplicationScheme;
            })
            .AddBearerToken(IdentityConstants.BearerScheme)
            .AddIdentityCookies();

        return services.AddIdentityCore<TUser>(configure)
            .AddApiEndpoints();
    }

    /// <summary>
    /// Configures the application cookie.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
    /// <returns>The services.</returns>
    public static IServiceCollection ConfigureApplicationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
        => services.Configure(IdentityConstants.ApplicationScheme, configure);

    /// <summary>
    /// Configure the external cookie.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
    /// <returns>The services.</returns>
    public static IServiceCollection ConfigureExternalCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
        => services.Configure(IdentityConstants.ExternalScheme, configure);

    private sealed class PostConfigureSecurityStampValidatorOptions : IPostConfigureOptions<SecurityStampValidatorOptions>
    {
        public PostConfigureSecurityStampValidatorOptions(TimeProvider? timeProvider = null)
        {
            // We could assign this to "timeProvider ?? TimeProvider.System", but
            // SecurityStampValidator already has system clock fallback logic.
            TimeProvider = timeProvider;
        }

        private TimeProvider? TimeProvider { get; }

        public void PostConfigure(string? name, SecurityStampValidatorOptions options)
        {
            options.TimeProvider ??= TimeProvider;
        }
    }

    private sealed class CompositeIdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : SignInAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var bearerResult = await Context.AuthenticateAsync(IdentityConstants.BearerScheme);

            // Only try to authenticate with the application cookie if there is no bearer token.
            if (!bearerResult.None)
            {
                return bearerResult;
            }

            // Cookie auth will return AuthenticateResult.NoResult() like bearer auth just did if there is no cookie.
            return await Context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        }

        protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }

        protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }
    }
}
