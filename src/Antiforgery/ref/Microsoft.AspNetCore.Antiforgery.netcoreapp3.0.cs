// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery
{
    public partial class AntiforgeryOptions
    {
        public static readonly string DefaultCookiePrefix;
        public AntiforgeryOptions() { }
        public Microsoft.AspNetCore.Http.CookieBuilder Cookie { get { throw null; } set { } }
        public string FormFieldName { get { throw null; } set { } }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SuppressXFrameOptionsHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class AntiforgeryTokenSet
    {
        public AntiforgeryTokenSet(string requestToken, string cookieToken, string formFieldName, string headerName) { }
        public string CookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string FormFieldName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string RequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class AntiforgeryValidationException : System.Exception
    {
        public AntiforgeryValidationException(string message) { }
        public AntiforgeryValidationException(string message, System.Exception innerException) { }
    }
    public partial interface IAntiforgery
    {
        Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext);
        Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
        void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    public partial interface IAntiforgeryAdditionalDataProvider
    {
        string GetAdditionalData(Microsoft.AspNetCore.Http.HttpContext context);
        bool ValidateAdditionalData(Microsoft.AspNetCore.Http.HttpContext context, string additionalData);
    }
}
namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public partial class AntiforgeryFeature : Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryFeature
    {
        public AntiforgeryFeature() { }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken CookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveDeserializedCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveDeserializedRequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveGeneratedNewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveStoredNewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken NewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string NewCookieTokenString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken NewRequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string NewRequestTokenString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken RequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class AntiforgeryOptionsSetup : Microsoft.Extensions.Options.ConfigureOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>
    {
        public AntiforgeryOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.DataProtectionOptions> dataProtectionOptionsAccessor) : base (default(System.Action<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>)) { }
        public static void ConfigureOptions(Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions options, Microsoft.AspNetCore.DataProtection.DataProtectionOptions dataProtectionOptions) { }
    }
    public partial class AntiforgerySerializationContext
    {
        public AntiforgerySerializationContext() { }
        public System.IO.BinaryReader Reader { get { throw null; } }
        public System.Security.Cryptography.SHA256 Sha256 { get { throw null; } }
        public System.IO.MemoryStream Stream { get { throw null; } }
        public System.IO.BinaryWriter Writer { get { throw null; } }
        public char[] GetChars(int count) { throw null; }
        public void Reset() { }
    }
    public partial class AntiforgerySerializationContextPooledObjectPolicy : Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<Microsoft.AspNetCore.Antiforgery.Internal.AntiforgerySerializationContext>
    {
        public AntiforgerySerializationContextPooledObjectPolicy() { }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgerySerializationContext Create() { throw null; }
        public bool Return(Microsoft.AspNetCore.Antiforgery.Internal.AntiforgerySerializationContext obj) { throw null; }
    }
    public sealed partial class AntiforgeryToken
    {
        public AntiforgeryToken() { }
        public string AdditionalData { get { throw null; } set { } }
        public Microsoft.AspNetCore.Antiforgery.Internal.BinaryBlob ClaimUid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.Internal.BinaryBlob SecurityToken { get { throw null; } set { } }
        public string Username { get { throw null; } set { } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerString}")]
    public sealed partial class BinaryBlob : System.IEquatable<Microsoft.AspNetCore.Antiforgery.Internal.BinaryBlob>
    {
        public BinaryBlob(int bitLength) { }
        public BinaryBlob(int bitLength, byte[] data) { }
        public int BitLength { get { throw null; } }
        public bool Equals(Microsoft.AspNetCore.Antiforgery.Internal.BinaryBlob other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public byte[] GetData() { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public static partial class CryptographyAlgorithms
    {
        public static System.Security.Cryptography.SHA256 CreateSHA256() { throw null; }
    }
    public partial class DefaultAntiforgery : Microsoft.AspNetCore.Antiforgery.IAntiforgery
    {
        public DefaultAntiforgery(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> antiforgeryOptionsAccessor, Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenGenerator tokenGenerator, Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenSerializer tokenSerializer, Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenStore tokenStore, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        protected virtual void SetDoNotCacheHeaders(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class DefaultAntiforgeryAdditionalDataProvider : Microsoft.AspNetCore.Antiforgery.IAntiforgeryAdditionalDataProvider
    {
        public DefaultAntiforgeryAdditionalDataProvider() { }
        public virtual string GetAdditionalData(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public virtual bool ValidateAdditionalData(Microsoft.AspNetCore.Http.HttpContext context, string additionalData) { throw null; }
    }
    public partial class DefaultAntiforgeryTokenGenerator : Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenGenerator
    {
        public DefaultAntiforgeryTokenGenerator(Microsoft.AspNetCore.Antiforgery.Internal.IClaimUidExtractor claimUidExtractor, Microsoft.AspNetCore.Antiforgery.IAntiforgeryAdditionalDataProvider additionalDataProvider) { }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken GenerateCookieToken() { throw null; }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken GenerateRequestToken(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken) { throw null; }
        public bool IsCookieTokenValid(Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken) { throw null; }
        public bool TryValidateTokenSet(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken requestToken, out string message) { throw null; }
    }
    public partial class DefaultAntiforgeryTokenSerializer : Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenSerializer
    {
        public DefaultAntiforgeryTokenSerializer(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider provider, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Antiforgery.Internal.AntiforgerySerializationContext> pool) { }
        public Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken Deserialize(string serializedToken) { throw null; }
        public string Serialize(Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken token) { throw null; }
    }
    public partial class DefaultAntiforgeryTokenStore : Microsoft.AspNetCore.Antiforgery.Internal.IAntiforgeryTokenStore
    {
        public DefaultAntiforgeryTokenStore(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> optionsAccessor) { }
        public string GetCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet> GetRequestTokensAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public void SaveCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext, string token) { }
    }
    public partial class DefaultClaimUidExtractor : Microsoft.AspNetCore.Antiforgery.Internal.IClaimUidExtractor
    {
        public DefaultClaimUidExtractor(Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Antiforgery.Internal.AntiforgerySerializationContext> pool) { }
        public string ExtractClaimUid(System.Security.Claims.ClaimsPrincipal claimsPrincipal) { throw null; }
        public static System.Collections.Generic.IList<string> GetUniqueIdentifierParameters(System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity> claimsIdentities) { throw null; }
    }
    public partial interface IAntiforgeryFeature
    {
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken CookieToken { get; set; }
        bool HaveDeserializedCookieToken { get; set; }
        bool HaveDeserializedRequestToken { get; set; }
        bool HaveGeneratedNewCookieToken { get; set; }
        bool HaveStoredNewCookieToken { get; set; }
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken NewCookieToken { get; set; }
        string NewCookieTokenString { get; set; }
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken NewRequestToken { get; set; }
        string NewRequestTokenString { get; set; }
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken RequestToken { get; set; }
    }
    public partial interface IAntiforgeryTokenGenerator
    {
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken GenerateCookieToken();
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken GenerateRequestToken(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken);
        bool IsCookieTokenValid(Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken);
        bool TryValidateTokenSet(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken cookieToken, Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken requestToken, out string message);
    }
    public partial interface IAntiforgeryTokenSerializer
    {
        Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken Deserialize(string serializedToken);
        string Serialize(Microsoft.AspNetCore.Antiforgery.Internal.AntiforgeryToken token);
    }
    public partial interface IAntiforgeryTokenStore
    {
        string GetCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet> GetRequestTokensAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
        void SaveCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext, string token);
    }
    public partial interface IClaimUidExtractor
    {
        string ExtractClaimUid(System.Security.Claims.ClaimsPrincipal claimsPrincipal);
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AntiforgeryServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAntiforgery(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAntiforgery(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> setupAction) { throw null; }
    }
}
