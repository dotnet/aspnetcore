// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    public partial class ApiAuthorizationDbContext<TUser> : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<TUser>, IdentityServer4.EntityFramework.Interfaces.IPersistedGrantDbContext, System.IDisposable where TUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        public ApiAuthorizationDbContext(Microsoft.EntityFrameworkCore.DbContextOptions options, Microsoft.Extensions.Options.IOptions<IdentityServer4.EntityFramework.Options.OperationalStoreOptions> operationalStoreOptions) { }
        public Microsoft.EntityFrameworkCore.DbSet<IdentityServer4.EntityFramework.Entities.DeviceFlowCodes> DeviceFlowCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.EntityFrameworkCore.DbSet<IdentityServer4.EntityFramework.Entities.PersistedGrant> PersistedGrants { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        System.Threading.Tasks.Task<int> IdentityServer4.EntityFramework.Interfaces.IPersistedGrantDbContext.SaveChangesAsync() { throw null; }
        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder builder) { }
    }
    public partial class ApiAuthorizationOptions
    {
        public ApiAuthorizationOptions() { }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceCollection ApiResources { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientCollection Clients { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceCollection IdentityResources { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.IdentityModel.Tokens.SigningCredentials SigningCredential { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class ApiResourceBuilder
    {
        public ApiResourceBuilder() { }
        public ApiResourceBuilder(IdentityServer4.Models.ApiResource resource) { }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder AllowAllClients() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder ApiResource(string name) { throw null; }
        public IdentityServer4.Models.ApiResource Build() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder IdentityServerJwt(string name) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder ReplaceScopes(params string[] resourceScopes) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder WithApplicationProfile(string profile) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder WithScopes(params string[] resourceScopes) { throw null; }
    }
    public partial class ApiResourceCollection : System.Collections.ObjectModel.Collection<IdentityServer4.Models.ApiResource>
    {
        public ApiResourceCollection() { }
        public ApiResourceCollection(System.Collections.Generic.IList<IdentityServer4.Models.ApiResource> list) { }
        public IdentityServer4.Models.ApiResource this[string key] { get { throw null; } }
        public void AddApiResource(string name, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder> configure) { }
        public void AddIdentityServerJwt(string name, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiResourceBuilder> configure) { }
        public void AddRange(params IdentityServer4.Models.ApiResource[] resources) { }
        public void AddRange(System.Collections.Generic.IEnumerable<IdentityServer4.Models.ApiResource> resources) { }
    }
    public static partial class ApplicationProfiles
    {
        public const string API = "API";
        public const string IdentityServerJwt = "IdentityServerJwt";
        public const string IdentityServerSPA = "IdentityServerSPA";
        public const string NativeApp = "NativeApp";
        public const string SPA = "SPA";
    }
    public static partial class ApplicationProfilesPropertyNames
    {
        public const string Clients = "Clients";
        public const string Profile = "Profile";
        public const string Source = "Source";
    }
    public static partial class ApplicationProfilesPropertyValues
    {
        public const string AllowAllApplications = "*";
        public const string Configuration = "Configuration";
        public const string Default = "Default";
    }
    public partial class ClientBuilder
    {
        public ClientBuilder() { }
        public ClientBuilder(IdentityServer4.Models.Client client) { }
        public IdentityServer4.Models.Client Build() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder IdentityServerSPA(string clientId) { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder NativeApp(string clientId) { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder SPA(string clientId) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithApplicationProfile(string profile) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithClientId(string clientId) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithLogoutRedirectUri(string logoutUri) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithoutClientSecrets() { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithRedirectUri(string redirectUri) { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder WithScopes(params string[] scopes) { throw null; }
    }
    public partial class ClientCollection : System.Collections.ObjectModel.Collection<IdentityServer4.Models.Client>
    {
        public ClientCollection() { }
        public ClientCollection(System.Collections.Generic.IList<IdentityServer4.Models.Client> list) { }
        public IdentityServer4.Models.Client this[string key] { get { throw null; } }
        public void AddIdentityServerSPA(string clientId, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder> configure) { }
        public void AddNativeApp(string clientId, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder> configure) { }
        public void AddRange(params IdentityServer4.Models.Client[] clients) { }
        public void AddRange(System.Collections.Generic.IEnumerable<IdentityServer4.Models.Client> clients) { }
        public void AddSPA(string clientId, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ClientBuilder> configure) { }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("*", Attributes="[asp-apiauth-parameters]")]
    public partial class ClientParametersTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
    {
        public ClientParametersTagHelper(Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IClientRequestParametersProvider clientRequestParametersProvider) { }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("asp-apiauth-parameters")]
        public string ClientId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Process(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { }
    }
    public partial interface IClientRequestParametersProvider
    {
        System.Collections.Generic.IDictionary<string, string> GetClientParameters(Microsoft.AspNetCore.Http.HttpContext context, string clientId);
    }
    public partial class IdentityResourceBuilder
    {
        public IdentityResourceBuilder() { }
        public IdentityResourceBuilder(IdentityServer4.Models.IdentityResource resource) { }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder Address() { throw null; }
        public Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder AllowAllClients() { throw null; }
        public IdentityServer4.Models.IdentityResource Build() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder Email() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder OpenId() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder Phone() { throw null; }
        public static Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder Profile() { throw null; }
    }
    public partial class IdentityResourceCollection : System.Collections.ObjectModel.Collection<IdentityServer4.Models.IdentityResource>
    {
        public IdentityResourceCollection() { }
        public IdentityResourceCollection(System.Collections.Generic.IList<IdentityServer4.Models.IdentityResource> list) { }
        public IdentityServer4.Models.IdentityResource this[string key] { get { throw null; } }
        public void AddAddress() { }
        public void AddAddress(System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder> configure) { }
        public void AddEmail() { }
        public void AddEmail(System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder> configure) { }
        public void AddOpenId() { }
        public void AddOpenId(System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder> configure) { }
        public void AddPhone() { }
        public void AddPhone(System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder> configure) { }
        public void AddProfile() { }
        public void AddProfile(System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.IdentityResourceBuilder> configure) { }
        public void AddRange(params IdentityServer4.Models.IdentityResource[] identityResources) { }
        public void AddRange(System.Collections.Generic.IEnumerable<IdentityServer4.Models.IdentityResource> identityResources) { }
    }
    public partial class IdentityServerJwtConstants
    {
        public const string IdentityServerJwtBearerScheme = "IdentityServerJwtBearer";
        public const string IdentityServerJwtScheme = "IdentityServerJwt";
        public IdentityServerJwtConstants() { }
    }
}
namespace Microsoft.AspNetCore.Authentication
{
    public static partial class AuthenticationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerJwt(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class IdentityServerBuilderConfigurationExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddApiAuthorization<TUser, TContext>(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder) where TUser : class where TContext : Microsoft.EntityFrameworkCore.DbContext, IdentityServer4.EntityFramework.Interfaces.IPersistedGrantDbContext { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddApiAuthorization<TUser, TContext>(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder, System.Action<Microsoft.AspNetCore.ApiAuthorization.IdentityServer.ApiAuthorizationOptions> configure) where TUser : class where TContext : Microsoft.EntityFrameworkCore.DbContext, IdentityServer4.EntityFramework.Interfaces.IPersistedGrantDbContext { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddApiResources(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddApiResources(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddClients(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddClients(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddIdentityResources(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddIdentityResources(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddSigningCredentials(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder AddSigningCredentials(this Microsoft.Extensions.DependencyInjection.IIdentityServerBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) { throw null; }
    }
}
