// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    public static partial class FacebookDefaults
    {
        public const string AuthenticationScheme = "Facebook";
        public static readonly string AuthorizationEndpoint;
        public static readonly string DisplayName;
        public static readonly string TokenEndpoint;
        public static readonly string UserInformationEndpoint;
    }
    public partial class FacebookHandler : Microsoft.AspNetCore.Authentication.OAuth.OAuthHandler<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions>
    {
        public FacebookHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> CreateTicketAsync(System.Security.Claims.ClaimsIdentity identity, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.OAuth.OAuthTokenResponse tokens) { throw null; }
        protected override string FormatScope() { throw null; }
        protected override string FormatScope(System.Collections.Generic.IEnumerable<string> scopes) { throw null; }
    }
    public partial class FacebookOptions : Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions
    {
        public FacebookOptions() { }
        public string AppId { get { throw null; } set { } }
        public string AppSecret { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Fields { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool SendAppSecretProof { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Validate() { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class FacebookAuthenticationOptionsExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddFacebook(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddFacebook(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddFacebook(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddFacebook(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.Facebook.FacebookOptions> configureOptions) { throw null; }
    }
}
