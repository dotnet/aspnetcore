using BlazorWeb_CSharp.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlazorWeb_CSharp;

public class PersistingAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly PersistingComponentStateSubscription _subscription;

    public PersistingAuthenticationStateProvider(IHttpContextAccessor contextAccessor, PersistentComponentState state, IOptions<IdentityOptions> identityOptions)
    {
        _contextAccessor = contextAccessor;

        _subscription = state.RegisterOnPersisting(() =>
        {
            var user = RequiredHttpContext.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(identityOptions.Value.ClaimsIdentity.UserIdClaimType)?.Value;
                var email = user.FindFirst(identityOptions.Value.ClaimsIdentity.EmailClaimType)?.Value;

                if (userId != null && email != null)
                {
                    state.PersistAsJson(nameof(UserInfo), new UserInfo
                    {
                        UserId = userId,
                        Email = email,
                    });
                }
            }

            return Task.CompletedTask;
        });
    }

    private HttpContext RequiredHttpContext => 
        _contextAccessor.HttpContext ?? throw new InvalidOperationException("IHttpContextAccessor HttpContext AsyncLocal missing!"); 

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(RequiredHttpContext.User));

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
