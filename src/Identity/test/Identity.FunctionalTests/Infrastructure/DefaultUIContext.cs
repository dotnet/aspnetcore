// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class DefaultUIContext : HtmlPageContext
{
    public DefaultUIContext() { }

    public DefaultUIContext(DefaultUIContext currentContext) : base(currentContext) { }

    public DefaultUIContext WithAuthenticatedUser() =>
        new DefaultUIContext(this) { UserAuthenticated = true };

    public DefaultUIContext WithAnonymousUser() =>
        new DefaultUIContext(this) { UserAuthenticated = false };

    public DefaultUIContext WithSocialLoginEnabled() =>
        new DefaultUIContext(this) { ContosoLoginEnabled = true };

    public DefaultUIContext WithExistingUser() =>
        new DefaultUIContext(this) { ExistingUser = true };

    public DefaultUIContext WithConfirmedEmail() =>
        new DefaultUIContext(this) { EmailConfirmed = true };

    internal DefaultUIContext WithSocialLoginProvider() =>
        new DefaultUIContext(this) { SocialLoginProvider = "contoso" };

    internal DefaultUIContext WithPasswordLogin() =>
        new DefaultUIContext(this) { PasswordLoginEnabled = true };

    internal DefaultUIContext WithCookieConsent() =>
        new DefaultUIContext(this) { CookiePolicyAccepted = true };

    internal DefaultUIContext WithRealEmailSender() =>
        new DefaultUIContext(this) { HasRealEmailSender = true };

    public string AuthenticatorKey
    {
        get => GetValue<string>(nameof(AuthenticatorKey));
        set => SetValue(nameof(AuthenticatorKey), value);
    }

    public string SocialLoginProvider
    {
        get => GetValue<string>(nameof(SocialLoginProvider));
        set => SetValue(nameof(SocialLoginProvider), value);
    }

    public string[] RecoveryCodes
    {
        get => GetValue<string[]>(nameof(RecoveryCodes));
        set => SetValue(nameof(RecoveryCodes), value);
    }

    public bool TwoFactorEnabled
    {
        get => GetValue<bool>(nameof(TwoFactorEnabled));
        set => SetValue(nameof(TwoFactorEnabled), value);
    }
    public bool ContosoLoginEnabled
    {
        get => GetValue<bool>(nameof(ContosoLoginEnabled));
        set => SetValue(nameof(ContosoLoginEnabled), value);
    }

    public bool UserAuthenticated
    {
        get => GetValue<bool>(nameof(UserAuthenticated));
        set => SetValue(nameof(UserAuthenticated), value);
    }

    public bool ExistingUser
    {
        get => GetValue<bool>(nameof(ExistingUser));
        set => SetValue(nameof(ExistingUser), value);
    }

    public bool EmailConfirmed
    {
        get => GetValue<bool>(nameof(ExistingUser));
        set => SetValue(nameof(ExistingUser), value);
    }

    public bool PasswordLoginEnabled
    {
        get => GetValue<bool>(nameof(PasswordLoginEnabled));
        set => SetValue(nameof(PasswordLoginEnabled), value);
    }

    public bool CookiePolicyAccepted
    {
        get => GetValue<bool>(nameof(CookiePolicyAccepted));
        set => SetValue(nameof(CookiePolicyAccepted), value);
    }

    public bool HasRealEmailSender
    {
        get => GetValue<bool>(nameof(HasRealEmailSender));
        set => SetValue(nameof(HasRealEmailSender), value);
    }
}
