// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery
{
    internal partial class AntiforgeryFeature : Microsoft.AspNetCore.Antiforgery.IAntiforgeryFeature
    {
        public AntiforgeryFeature() { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken CookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveDeserializedCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveDeserializedRequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveGeneratedNewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HaveStoredNewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken NewCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string NewCookieTokenString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken NewRequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string NewRequestTokenString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken RequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }

    internal partial class AntiforgerySerializationContext
    {
        public AntiforgerySerializationContext() { }
        public System.IO.BinaryReader Reader { get { throw null; } }
        public System.Security.Cryptography.SHA256 Sha256 { get { throw null; } }
        public System.IO.MemoryStream Stream { get { throw null; } }
        public System.IO.BinaryWriter Writer { get { throw null; } }
        public char[] GetChars(int count) { throw null; }
        public void Reset() { }
    }

    internal partial class AntiforgerySerializationContextPooledObjectPolicy : Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext>
    {
        public AntiforgerySerializationContextPooledObjectPolicy() { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext Create() { throw null; }
        public bool Return(Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext obj) { throw null; }
    }

    internal sealed partial class AntiforgeryToken
    {
        internal const int ClaimUidBitLength = 256;
        internal const int SecurityTokenBitLength = 128;
        public AntiforgeryToken() { }
        public string AdditionalData { get { throw null; } set { } }
        public Microsoft.AspNetCore.Antiforgery.BinaryBlob ClaimUid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsCookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Antiforgery.BinaryBlob SecurityToken { get { throw null; } set { } }
        public string Username { get { throw null; } set { } }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerString}")]
    internal sealed partial class BinaryBlob : System.IEquatable<Microsoft.AspNetCore.Antiforgery.BinaryBlob>
    {
        public BinaryBlob(int bitLength) { }
        public BinaryBlob(int bitLength, byte[] data) { }
        public int BitLength { get { throw null; } }
        public bool Equals(Microsoft.AspNetCore.Antiforgery.BinaryBlob other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public byte[] GetData() { throw null; }
        public override int GetHashCode() { throw null; }
    }

    internal partial class DefaultAntiforgery : Microsoft.AspNetCore.Antiforgery.IAntiforgery
    {
        public DefaultAntiforgery(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> antiforgeryOptionsAccessor, Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenGenerator tokenGenerator, Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenSerializer tokenSerializer, Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenStore tokenStore, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        protected virtual void SetDoNotCacheHeaders(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }

    internal partial class DefaultAntiforgeryTokenGenerator : Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenGenerator
    {
        public DefaultAntiforgeryTokenGenerator(Microsoft.AspNetCore.Antiforgery.IClaimUidExtractor claimUidExtractor, Microsoft.AspNetCore.Antiforgery.IAntiforgeryAdditionalDataProvider additionalDataProvider) { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken GenerateCookieToken() { throw null; }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken GenerateRequestToken(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken) { throw null; }
        public bool IsCookieTokenValid(Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken) { throw null; }
        public bool TryValidateTokenSet(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken requestToken, out string message) { throw null; }
    }

    internal partial class DefaultAntiforgeryTokenSerializer : Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenSerializer
    {
        public DefaultAntiforgeryTokenSerializer(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider provider, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext> pool) { }
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryToken Deserialize(string serializedToken) { throw null; }
        public string Serialize(Microsoft.AspNetCore.Antiforgery.AntiforgeryToken token) { throw null; }
    }

    internal partial class DefaultAntiforgeryTokenStore : Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenStore
    {
        public DefaultAntiforgeryTokenStore(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> optionsAccessor) { }
        public string GetCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet> GetRequestTokensAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public void SaveCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext, string token) { }
    }

    internal partial class DefaultClaimUidExtractor : Microsoft.AspNetCore.Antiforgery.IClaimUidExtractor
    {
        public DefaultClaimUidExtractor(Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext> pool) { }
        public string ExtractClaimUid(System.Security.Claims.ClaimsPrincipal claimsPrincipal) { throw null; }
        public static System.Collections.Generic.IList<string> GetUniqueIdentifierParameters(System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity> claimsIdentities) { throw null; }
    }

    internal partial interface IAntiforgeryFeature
    {
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken CookieToken { get; set; }
        bool HaveDeserializedCookieToken { get; set; }
        bool HaveDeserializedRequestToken { get; set; }
        bool HaveGeneratedNewCookieToken { get; set; }
        bool HaveStoredNewCookieToken { get; set; }
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken NewCookieToken { get; set; }
        string NewCookieTokenString { get; set; }
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken NewRequestToken { get; set; }
        string NewRequestTokenString { get; set; }
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken RequestToken { get; set; }
    }

    internal partial interface IAntiforgeryTokenGenerator
    {
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken GenerateCookieToken();
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken GenerateRequestToken(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken);
        bool IsCookieTokenValid(Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken);
        bool TryValidateTokenSet(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken cookieToken, Microsoft.AspNetCore.Antiforgery.AntiforgeryToken requestToken, out string message);
    }

    internal partial interface IAntiforgeryTokenSerializer
    {
        Microsoft.AspNetCore.Antiforgery.AntiforgeryToken Deserialize(string serializedToken);
        string Serialize(Microsoft.AspNetCore.Antiforgery.AntiforgeryToken token);
    }

    internal partial interface IAntiforgeryTokenStore
    {
        string GetCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet> GetRequestTokensAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
        void SaveCookieToken(Microsoft.AspNetCore.Http.HttpContext httpContext, string token);
    }

    internal partial interface IClaimUidExtractor
    {
        string ExtractClaimUid(System.Security.Claims.ClaimsPrincipal claimsPrincipal);
    }
}
