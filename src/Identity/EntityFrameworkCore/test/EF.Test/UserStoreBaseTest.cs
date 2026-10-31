// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Moq.Protected;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class UserStoreBaseTest
{
    [Theory]
    [InlineData("LoginProvider", "ProviderKey", true)]
    [InlineData("loginprovider", "ProviderKey", false)]
    [InlineData("LoginProvider", "providerkey", false)]
    public async Task FindByLoginAsyncRequiresOrdinalMatch(
        string loginProvider,
        string providerKey,
        bool expected)
    {
        var user = new IdentityUser();
        var userLogin = new IdentityUserLogin<string>
        {
            UserId = user.Id,
            LoginProvider = "LoginProvider",
            ProviderKey = "ProviderKey",
        };
        var store = new Mock<UserStoreBase<
            IdentityUser,
            string,
            IdentityUserClaim<string>,
            IdentityUserLogin<string>,
            IdentityUserToken<string>>>(new IdentityErrorDescriber())
        {
            CallBase = true,
        };
        store.Protected()
            .Setup<Task<IdentityUserLogin<string>>>(
                "FindUserLoginAsync",
                loginProvider,
                providerKey,
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(userLogin);
        store.Protected()
            .Setup<Task<IdentityUser>>(
                "FindUserAsync",
                user.Id,
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(user);

        Assert.Equal(
            expected,
            await store.Object.FindByLoginAsync(loginProvider, providerKey) is not null);
    }
}
