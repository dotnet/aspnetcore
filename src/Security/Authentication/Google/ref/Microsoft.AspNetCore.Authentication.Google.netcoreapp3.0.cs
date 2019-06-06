// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Google
{
    public partial class GoogleChallengeProperties : Microsoft.AspNetCore.Authentication.OAuth.OAuthChallengeProperties
    {
        public static readonly string AccessTypeKey;
        public static readonly string ApprovalPromptKey;
        public static readonly string IncludeGrantedScopesKey;
        public static readonly string LoginHintKey;
        public static readonly string PromptParameterKey;
        public GoogleChallengeProperties() { }
        public GoogleChallengeProperties(System.Collections.Generic.IDictionary<string, string> items) { }
        public GoogleChallengeProperties(System.Collections.Generic.IDictionary<string, string> items, System.Collections.Generic.IDictionary<string, object> parameters) { }
        public string AccessType { get { throw null; } set { } }
        public string ApprovalPrompt { get { throw null; } set { } }
        public bool? IncludeGrantedScopes { get { throw null; } set { } }
        public string LoginHint { get { throw null; } set { } }
        public string Prompt { get { throw null; } set { } }
    }
    public static partial class GoogleDefaults
    {
        public const string AuthenticationScheme = "Google";
        public static readonly string AuthorizationEndpoint;
        public static readonly string DisplayName;
        public static readonly string TokenEndpoint;
        public static readonly string UserInformationEndpoint;
    }
    public partial class GoogleHandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>
    {
        public GoogleHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected override string BuildChallengeUrl(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string redirectUri) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> CreateTicketAsync(System.Security.Claims.ClaimsIdentity identity, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse tokens) { throw null; }
    }
    public partial class GoogleOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions
    {
        public GoogleOptions() { }
        public string AccessType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class GoogleExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddGoogle(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddGoogle(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddGoogle(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddGoogle(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> configureOptions) { throw null; }
    }
}
