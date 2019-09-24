// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{
    public static partial class MicrosoftAccountDefaults
    {
        public const string AuthenticationScheme = "Microsoft";
        public static readonly string AuthorizationEndpoint;
        public static readonly string DisplayName;
        public static readonly string TokenEndpoint;
        public static readonly string UserInformationEndpoint;
    }
    public partial class MicrosoftAccountHandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions>
    {
        public MicrosoftAccountHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected override string BuildChallengeUrl(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string redirectUri) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> CreateTicketAsync(System.Security.Claims.ClaimsIdentity identity, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse tokens) { throw null; }
    }
    public partial class MicrosoftAccountOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions
    {
        public MicrosoftAccountOptions() { }
    }
    public partial class MicrosoftChallengeProperties : Microsoft.AspNetCore.Authentication.OAuth.OAuthChallengeProperties
    {
        public static readonly string DomainHintKey;
        public static readonly string LoginHintKey;
        public static readonly string PromptKey;
        public static readonly string ResponseModeKey;
        public MicrosoftChallengeProperties() { }
        public MicrosoftChallengeProperties(System.Collections.Generic.IDictionary<string, string> items) { }
        public MicrosoftChallengeProperties(System.Collections.Generic.IDictionary<string, string> items, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public string DomainHint { get { throw null; } set { } }
        public string LoginHint { get { throw null; } set { } }
        public string Prompt { get { throw null; } set { } }
        public string ResponseMode { get { throw null; } set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MicrosoftAccountExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddMicrosoftAccount(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddMicrosoftAccount(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddMicrosoftAccount(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddMicrosoftAccount(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions> configureOptions) { throw null; }
    }
}
