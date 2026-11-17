using BlazorWebCSharp._1.Data;
using Microsoft.AspNetCore.Identity;

namespace BlazorWebCSharp._1.Components.Account;

internal static class PasskeyUpgradeManager
{
    public const string CreationOptionsKey = "Identity.PasskeyUpgradeCreationOptions";

    public static async Task<string?> TryMakeCreationOptionsAsync(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser user)
    {
        if (!signInManager.SupportsPasskeyConditionalCreation)
        {
            return null;
        }

        var userId = await userManager.GetUserIdAsync(user);
        var userName = await userManager.GetUserNameAsync(user) ?? "User";
        return await signInManager.MakePasskeyCreationOptionsAsync(new()
        {
            Id = userId,
            Name = userName,
            DisplayName = userName
        }, isConditionallyMediated: true);
    }
}
