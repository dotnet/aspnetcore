// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public partial class AccessToken : Microsoft.AspNetCore.Authentication.Twitter.RequestToken
    {
        public AccessToken() { }
        public string ScreenName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RequestToken
    {
        public RequestToken() { }
        public bool CallbackConfirmed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Token { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TokenSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RequestTokenSerializer : Microsoft.AspNetCore.Authentication.IDataSerializer<Microsoft.AspNetCore.Authentication.Twitter.RequestToken>
    {
        public RequestTokenSerializer() { }
        public virtual Microsoft.AspNetCore.Authentication.Twitter.RequestToken Deserialize(byte[] data) { throw null; }
        public static Microsoft.AspNetCore.Authentication.Twitter.RequestToken Read(System.IO.BinaryReader reader) { throw null; }
        public virtual byte[] Serialize(Microsoft.AspNetCore.Authentication.Twitter.RequestToken model) { throw null; }
        public static void Write(System.IO.BinaryWriter writer, Microsoft.AspNetCore.Authentication.Twitter.RequestToken token) { }
    }
    public partial class TwitterCreatingTicketContext : Microsoft.AspNetCore.Authentication.ResultContext<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions>
    {
        public TwitterCreatingTicketContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions options, System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, string userId, string screenName, string accessToken, string accessTokenSecret, System.Text.Json.JsonElement user) : base (default(Microsoft.AspNetCore.Http.HttpContext), default(Microsoft.AspNetCore.Authentication.AuthenticationScheme), default(Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions)) { }
        public string AccessToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string AccessTokenSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ScreenName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Text.Json.JsonElement User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class TwitterDefaults
    {
        public const string AuthenticationScheme = "Twitter";
        public static readonly string DisplayName;
    }
    public partial class TwitterEvents : Microsoft.AspNetCore.Authentication.RemoteAuthenticationEvents
    {
        public TwitterEvents() { }
        public System.Func<Microsoft.AspNetCore.Authentication.Twitter.TwitterCreatingTicketContext, System.Threading.Tasks.Task> OnCreatingTicket { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions>, System.Threading.Tasks.Task> OnRedirectToAuthorizationEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task CreatingTicket(Microsoft.AspNetCore.Authentication.Twitter.TwitterCreatingTicketContext context) { throw null; }
        public virtual System.Threading.Tasks.Task RedirectToAuthorizationEndpoint(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions> context) { throw null; }
    }
    public partial class TwitterHandler : Microsoft.AspNetCore.Authentication.RemoteAuthenticationHandler<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions>
    {
        public TwitterHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock) : base (default(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions>), default(Microsoft.Extensions.Logging.ILoggerFactory), default(System.Text.Encodings.Web.UrlEncoder), default(Microsoft.AspNetCore.Authentication.ISystemClock)) { }
        protected new Microsoft.AspNetCore.Authentication.Twitter.TwitterEvents Events { get { throw null; } set { } }
        protected override System.Threading.Tasks.Task<object> CreateEventsAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticationTicket> CreateTicketAsync(System.Security.Claims.ClaimsIdentity identity, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, Microsoft.AspNetCore.Authentication.Twitter.AccessToken token, System.Text.Json.JsonElement user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.HandleRequestResult> HandleRemoteAuthenticateAsync() { throw null; }
    }
    public partial class TwitterOptions : Microsoft.AspNetCore.Authentication.RemoteAuthenticationOptions
    {
        public TwitterOptions() { }
        public Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection ClaimActions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ConsumerKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ConsumerSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public new Microsoft.AspNetCore.Authentication.Twitter.TwitterEvents Events { get { throw null; } set { } }
        public bool RetrieveUserDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.CookieBuilder StateCookie { get { throw null; } set { } }
        public Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.Twitter.RequestToken> StateDataFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Validate() { }
    }
    public partial class TwitterPostConfigureOptions : Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions>
    {
        public TwitterPostConfigureOptions(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtection) { }
        public void PostConfigure(string name, Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions options) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class TwitterExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddTwitter(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddTwitter(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddTwitter(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, System.Action<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddTwitter(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string authenticationScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.Twitter.TwitterOptions> configureOptions) { throw null; }
    }
}
