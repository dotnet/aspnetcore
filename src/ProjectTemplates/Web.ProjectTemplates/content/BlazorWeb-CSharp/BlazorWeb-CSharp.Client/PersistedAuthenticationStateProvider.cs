using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorWeb_CSharp.Client;

public class PersistedAuthenticationStateProvider(PersistentComponentState persistentState) : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // REVIEW: Is TryTakeFromJson correctly annotated? "|| userInfo is null" should not be necessary.
        if (!persistentState.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            return _unauthenticatedTask;
        }

        Claim[] claims = [
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.Email),
            new Claim(ClaimTypes.Email, userInfo.Email) ];

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                authenticationType: nameof(PersistedAuthenticationStateProvider)))));
    }
}

