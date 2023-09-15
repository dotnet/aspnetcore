using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Identity;

internal static class UserManagerExtensions
{
    public static async Task<(TUser? User, string? Error)> GetUserAsync<TUser>(this UserManager<TUser> userManager, Task<AuthenticationState>? authenticationStateTask)
        where TUser : class
    {
        if (authenticationStateTask is null)
        {
            return (User: null, Error: "Unable to authenticate user.");
        }

        var authenticationState = await authenticationStateTask;
        var user = await userManager.GetUserAsync(authenticationState.User);
        if (user is null)
        {
            return (User: null, Error: $"Unable to load user with ID '{userManager.GetUserId(authenticationState.User)}'.");
        }

        return (User: user, Error: null);
    }
}
