# Microsoft.AspNetCore.Antiforgery.Internal

``` diff
-namespace Microsoft.AspNetCore.Antiforgery.Internal {
 {
-    public class AntiforgeryFeature : IAntiforgeryFeature {
 {
-        public AntiforgeryFeature();

-        public AntiforgeryToken CookieToken { get; set; }

-        public bool HaveDeserializedCookieToken { get; set; }

-        public bool HaveDeserializedRequestToken { get; set; }

-        public bool HaveGeneratedNewCookieToken { get; set; }

-        public bool HaveStoredNewCookieToken { get; set; }

-        public AntiforgeryToken NewCookieToken { get; set; }

-        public string NewCookieTokenString { get; set; }

-        public AntiforgeryToken NewRequestToken { get; set; }

-        public string NewRequestTokenString { get; set; }

-        public AntiforgeryToken RequestToken { get; set; }

-    }
-    public class AntiforgeryOptionsSetup : ConfigureOptions<AntiforgeryOptions> {
 {
-        public AntiforgeryOptionsSetup(IOptions<DataProtectionOptions> dataProtectionOptionsAccessor);

-        public static void ConfigureOptions(AntiforgeryOptions options, DataProtectionOptions dataProtectionOptions);

-    }
-    public class AntiforgerySerializationContext {
 {
-        public AntiforgerySerializationContext();

-        public BinaryReader Reader { get; private set; }

-        public SHA256 Sha256 { get; private set; }

-        public MemoryStream Stream { get; private set; }

-        public BinaryWriter Writer { get; private set; }

-        public char[] GetChars(int count);

-        public void Reset();

-    }
-    public class AntiforgerySerializationContextPooledObjectPolicy : IPooledObjectPolicy<AntiforgerySerializationContext> {
 {
-        public AntiforgerySerializationContextPooledObjectPolicy();

-        public AntiforgerySerializationContext Create();

-        public bool Return(AntiforgerySerializationContext obj);

-    }
-    public sealed class AntiforgeryToken {
 {
-        public AntiforgeryToken();

-        public string AdditionalData { get; set; }

-        public BinaryBlob ClaimUid { get; set; }

-        public bool IsCookieToken { get; set; }

-        public BinaryBlob SecurityToken { get; set; }

-        public string Username { get; set; }

-    }
-    public sealed class BinaryBlob : IEquatable<BinaryBlob> {
 {
-        public BinaryBlob(int bitLength);

-        public BinaryBlob(int bitLength, byte[] data);

-        public int BitLength { get; }

-        public bool Equals(BinaryBlob other);

-        public override bool Equals(object obj);

-        public byte[] GetData();

-        public override int GetHashCode();

-    }
-    public static class CryptographyAlgorithms {
 {
-        public static SHA256 CreateSHA256();

-    }
-    public class DefaultAntiforgery : IAntiforgery {
 {
-        public DefaultAntiforgery(IOptions<AntiforgeryOptions> antiforgeryOptionsAccessor, IAntiforgeryTokenGenerator tokenGenerator, IAntiforgeryTokenSerializer tokenSerializer, IAntiforgeryTokenStore tokenStore, ILoggerFactory loggerFactory);

-        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext);

-        public AntiforgeryTokenSet GetTokens(HttpContext httpContext);

-        public Task<bool> IsRequestValidAsync(HttpContext httpContext);

-        public void SetCookieTokenAndHeader(HttpContext httpContext);

-        protected virtual void SetDoNotCacheHeaders(HttpContext httpContext);

-        public Task ValidateRequestAsync(HttpContext httpContext);

-    }
-    public class DefaultAntiforgeryAdditionalDataProvider : IAntiforgeryAdditionalDataProvider {
 {
-        public DefaultAntiforgeryAdditionalDataProvider();

-        public virtual string GetAdditionalData(HttpContext context);

-        public virtual bool ValidateAdditionalData(HttpContext context, string additionalData);

-    }
-    public class DefaultAntiforgeryTokenGenerator : IAntiforgeryTokenGenerator {
 {
-        public DefaultAntiforgeryTokenGenerator(IClaimUidExtractor claimUidExtractor, IAntiforgeryAdditionalDataProvider additionalDataProvider);

-        public AntiforgeryToken GenerateCookieToken();

-        public AntiforgeryToken GenerateRequestToken(HttpContext httpContext, AntiforgeryToken cookieToken);

-        public bool IsCookieTokenValid(AntiforgeryToken cookieToken);

-        public bool TryValidateTokenSet(HttpContext httpContext, AntiforgeryToken cookieToken, AntiforgeryToken requestToken, out string message);

-    }
-    public class DefaultAntiforgeryTokenSerializer : IAntiforgeryTokenSerializer {
 {
-        public DefaultAntiforgeryTokenSerializer(IDataProtectionProvider provider, ObjectPool<AntiforgerySerializationContext> pool);

-        public AntiforgeryToken Deserialize(string serializedToken);

-        public string Serialize(AntiforgeryToken token);

-    }
-    public class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore {
 {
-        public DefaultAntiforgeryTokenStore(IOptions<AntiforgeryOptions> optionsAccessor);

-        public string GetCookieToken(HttpContext httpContext);

-        public Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext);

-        public void SaveCookieToken(HttpContext httpContext, string token);

-    }
-    public class DefaultClaimUidExtractor : IClaimUidExtractor {
 {
-        public DefaultClaimUidExtractor(ObjectPool<AntiforgerySerializationContext> pool);

-        public string ExtractClaimUid(ClaimsPrincipal claimsPrincipal);

-        public static IList<string> GetUniqueIdentifierParameters(IEnumerable<ClaimsIdentity> claimsIdentities);

-    }
-    public interface IAntiforgeryFeature {
 {
-        AntiforgeryToken CookieToken { get; set; }

-        bool HaveDeserializedCookieToken { get; set; }

-        bool HaveDeserializedRequestToken { get; set; }

-        bool HaveGeneratedNewCookieToken { get; set; }

-        bool HaveStoredNewCookieToken { get; set; }

-        AntiforgeryToken NewCookieToken { get; set; }

-        string NewCookieTokenString { get; set; }

-        AntiforgeryToken NewRequestToken { get; set; }

-        string NewRequestTokenString { get; set; }

-        AntiforgeryToken RequestToken { get; set; }

-    }
-    public interface IAntiforgeryTokenGenerator {
 {
-        AntiforgeryToken GenerateCookieToken();

-        AntiforgeryToken GenerateRequestToken(HttpContext httpContext, AntiforgeryToken cookieToken);

-        bool IsCookieTokenValid(AntiforgeryToken cookieToken);

-        bool TryValidateTokenSet(HttpContext httpContext, AntiforgeryToken cookieToken, AntiforgeryToken requestToken, out string message);

-    }
-    public interface IAntiforgeryTokenSerializer {
 {
-        AntiforgeryToken Deserialize(string serializedToken);

-        string Serialize(AntiforgeryToken token);

-    }
-    public interface IAntiforgeryTokenStore {
 {
-        string GetCookieToken(HttpContext httpContext);

-        Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext);

-        void SaveCookieToken(HttpContext httpContext, string token);

-    }
-    public interface IClaimUidExtractor {
 {
-        string ExtractClaimUid(ClaimsPrincipal claimsPrincipal);

-    }
-}
```

