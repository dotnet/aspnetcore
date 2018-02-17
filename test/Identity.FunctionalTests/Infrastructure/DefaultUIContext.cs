using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class DefaultUIContext : HtmlPageContext
    {
        public DefaultUIContext()
        {
        }

        public DefaultUIContext(DefaultUIContext currentContext)
            : base(currentContext)
        {

        }

        public DefaultUIContext WithAuthenticatedUser() =>
            new DefaultUIContext(this) { UserAuthenticated = true };

        public DefaultUIContext WithSocialLoginEnabled() =>
            new DefaultUIContext(this) { ContosoLoginEnabled = true };

        public DefaultUIContext WithExistingUser() =>
            new DefaultUIContext(this) { ExistingUser = true };

        public string AuthenticatorKey
        {
            get => GetValue<string>(nameof(AuthenticatorKey));
            set => SetValue(nameof(AuthenticatorKey), value);
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
    }
}
