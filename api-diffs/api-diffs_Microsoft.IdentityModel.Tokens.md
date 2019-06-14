# Microsoft.IdentityModel.Tokens

``` diff
-namespace Microsoft.IdentityModel.Tokens {
 {
-    public abstract class AsymmetricSecurityKey : SecurityKey {
 {
-        protected AsymmetricSecurityKey();

-        public abstract bool HasPrivateKey { get; }

-        public abstract PrivateKeyStatus PrivateKeyStatus { get; }

-    }
-    public class AsymmetricSignatureProvider : SignatureProvider {
 {
-        public static readonly Dictionary<string, int> DefaultMinimumAsymmetricKeySizeInBitsForSigningMap;

-        public static readonly Dictionary<string, int> DefaultMinimumAsymmetricKeySizeInBitsForVerifyingMap;

-        public AsymmetricSignatureProvider(SecurityKey key, string algorithm);

-        public AsymmetricSignatureProvider(SecurityKey key, string algorithm, bool willCreateSignatures);

-        public IReadOnlyDictionary<string, int> MinimumAsymmetricKeySizeInBitsForSigningMap { get; }

-        public IReadOnlyDictionary<string, int> MinimumAsymmetricKeySizeInBitsForVerifyingMap { get; }

-        protected override void Dispose(bool disposing);

-        protected virtual HashAlgorithmName GetHashAlgorithmName(string algorithm);

-        public override byte[] Sign(byte[] input);

-        public virtual void ValidateAsymmetricSecurityKeySize(SecurityKey key, string algorithm, bool willCreateSignatures);

-        public override bool Verify(byte[] input, byte[] signature);

-    }
-    public delegate bool AudienceValidator(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters);

-    public class AuthenticatedEncryptionProvider {
 {
-        public AuthenticatedEncryptionProvider(SecurityKey key, string algorithm);

-        public string Algorithm { get; private set; }

-        public string Context { get; set; }

-        public SecurityKey Key { get; private set; }

-        public virtual byte[] Decrypt(byte[] ciphertext, byte[] authenticatedData, byte[] iv, byte[] authenticationTag);

-        public virtual AuthenticatedEncryptionResult Encrypt(byte[] plaintext, byte[] authenticatedData);

-        public virtual AuthenticatedEncryptionResult Encrypt(byte[] plaintext, byte[] authenticatedData, byte[] iv);

-        protected virtual byte[] GetKeyBytes(SecurityKey key);

-        protected virtual bool IsSupportedAlgorithm(SecurityKey key, string algorithm);

-        protected virtual void ValidateKeySize(SecurityKey key, string algorithm);

-    }
-    public class AuthenticatedEncryptionResult {
 {
-        public AuthenticatedEncryptionResult(SecurityKey key, byte[] ciphertext, byte[] iv, byte[] authenticationTag);

-        public byte[] AuthenticationTag { get; private set; }

-        public byte[] Ciphertext { get; private set; }

-        public byte[] IV { get; private set; }

-        public SecurityKey Key { get; private set; }

-    }
-    public static class Base64UrlEncoder {
 {
-        public static string Decode(string arg);

-        public static byte[] DecodeBytes(string str);

-        public static string Encode(byte[] inArray);

-        public static string Encode(byte[] inArray, int offset, int length);

-        public static string Encode(string arg);

-    }
-    public class CompressionAlgorithms {
 {
-        public const string Deflate = "DEF";

-        public CompressionAlgorithms();

-    }
-    public class CompressionProviderFactory {
 {
-        public CompressionProviderFactory();

-        public CompressionProviderFactory(CompressionProviderFactory other);

-        public ICompressionProvider CustomCompressionProvider { get; set; }

-        public static CompressionProviderFactory Default { get; set; }

-        public ICompressionProvider CreateCompressionProvider(string algorithm);

-        public virtual bool IsSupportedAlgorithm(string algorithm);

-    }
-    public abstract class CryptoProviderCache {
 {
-        protected CryptoProviderCache();

-        protected abstract string GetCacheKey(SecurityKey securityKey, string algorithm, string typeofProvider);

-        protected abstract string GetCacheKey(SignatureProvider signatureProvider);

-        public abstract bool TryAdd(SignatureProvider signatureProvider);

-        public abstract bool TryGetSignatureProvider(SecurityKey securityKey, string algorithm, string typeofProvider, bool willCreateSignatures, out SignatureProvider signatureProvider);

-        public abstract bool TryRemove(SignatureProvider signatureProvider);

-    }
-    public class CryptoProviderFactory {
 {
-        public CryptoProviderFactory();

-        public CryptoProviderFactory(CryptoProviderFactory other);

-        public bool CacheSignatureProviders { get; set; }

-        public CryptoProviderCache CryptoProviderCache { get; }

-        public ICryptoProvider CustomCryptoProvider { get; set; }

-        public static CryptoProviderFactory Default { get; set; }

-        public static bool DefaultCacheSignatureProviders { get; set; }

-        public virtual AuthenticatedEncryptionProvider CreateAuthenticatedEncryptionProvider(SecurityKey key, string algorithm);

-        public virtual SignatureProvider CreateForSigning(SecurityKey key, string algorithm);

-        public virtual SignatureProvider CreateForVerifying(SecurityKey key, string algorithm);

-        public virtual HashAlgorithm CreateHashAlgorithm(HashAlgorithmName algorithm);

-        public virtual HashAlgorithm CreateHashAlgorithm(string algorithm);

-        public virtual KeyedHashAlgorithm CreateKeyedHashAlgorithm(byte[] keyBytes, string algorithm);

-        public virtual KeyWrapProvider CreateKeyWrapProvider(SecurityKey key, string algorithm);

-        public virtual KeyWrapProvider CreateKeyWrapProviderForUnwrap(SecurityKey key, string algorithm);

-        public virtual bool IsSupportedAlgorithm(string algorithm);

-        public virtual bool IsSupportedAlgorithm(string algorithm, SecurityKey key);

-        public virtual void ReleaseHashAlgorithm(HashAlgorithm hashAlgorithm);

-        public virtual void ReleaseKeyWrapProvider(KeyWrapProvider provider);

-        public virtual void ReleaseRsaKeyWrapProvider(RsaKeyWrapProvider provider);

-        public virtual void ReleaseSignatureProvider(SignatureProvider signatureProvider);

-    }
-    public static class DateTimeUtil {
 {
-        public static DateTime Add(DateTime time, TimeSpan timespan);

-        public static DateTime GetMaxValue(DateTimeKind kind);

-        public static DateTime GetMinValue(DateTimeKind kind);

-        public static DateTime ToUniversalTime(DateTime value);

-        public static Nullable<DateTime> ToUniversalTime(Nullable<DateTime> value);

-    }
-    public class DeflateCompressionProvider : ICompressionProvider {
 {
-        public DeflateCompressionProvider();

-        public DeflateCompressionProvider(CompressionLevel compressionLevel);

-        public string Algorithm { get; }

-        public CompressionLevel CompressionLevel { get; private set; }

-        public byte[] Compress(byte[] value);

-        public byte[] Decompress(byte[] value);

-        public bool IsSupportedAlgorithm(string algorithm);

-    }
-    public class ECDsaSecurityKey : AsymmetricSecurityKey {
 {
-        public ECDsaSecurityKey(ECDsa ecdsa);

-        public ECDsa ECDsa { get; private set; }

-        public override bool HasPrivateKey { get; }

-        public override int KeySize { get; }

-        public override PrivateKeyStatus PrivateKeyStatus { get; }

-    }
-    public class EncryptingCredentials {
 {
-        public EncryptingCredentials(SecurityKey key, string alg, string enc);

-        public EncryptingCredentials(SymmetricSecurityKey key, string enc);

-        protected EncryptingCredentials(X509Certificate2 certificate, string alg, string enc);

-        public string Alg { get; private set; }

-        public CryptoProviderFactory CryptoProviderFactory { get; set; }

-        public string Enc { get; private set; }

-        public SecurityKey Key { get; private set; }

-    }
-    public static class EpochTime {
 {
-        public static readonly DateTime UnixEpoch;

-        public static DateTime DateTime(long secondsSinceUnixEpoch);

-        public static long GetIntDate(DateTime datetime);

-    }
-    public interface ICompressionProvider {
 {
-        string Algorithm { get; }

-        byte[] Compress(byte[] value);

-        byte[] Decompress(byte[] value);

-        bool IsSupportedAlgorithm(string algorithm);

-    }
-    public interface ICryptoProvider {
 {
-        object Create(string algorithm, params object[] args);

-        bool IsSupportedAlgorithm(string algorithm, params object[] args);

-        void Release(object cryptoInstance);

-    }
-    public class InMemoryCryptoProviderCache : CryptoProviderCache {
 {
-        public InMemoryCryptoProviderCache();

-        protected override string GetCacheKey(SecurityKey securityKey, string algorithm, string typeofProvider);

-        protected override string GetCacheKey(SignatureProvider signatureProvider);

-        public override bool TryAdd(SignatureProvider signatureProvider);

-        public override bool TryGetSignatureProvider(SecurityKey securityKey, string algorithm, string typeofProvider, bool willCreateSignatures, out SignatureProvider signatureProvider);

-        public override bool TryRemove(SignatureProvider signatureProvider);

-    }
-    public interface ISecurityTokenValidator {
 {
-        bool CanValidateToken { get; }

-        int MaximumTokenSizeInBytes { get; set; }

-        bool CanReadToken(string securityToken);

-        ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken);

-    }
-    public delegate IEnumerable<SecurityKey> IssuerSigningKeyResolver(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters);

-    public delegate bool IssuerSigningKeyValidator(SecurityKey securityKey, SecurityToken securityToken, TokenValidationParameters validationParameters);

-    public delegate string IssuerValidator(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters);

-    public interface ITokenReplayCache {
 {
-        bool TryAdd(string securityToken, DateTime expiresOn);

-        bool TryFind(string securityToken);

-    }
-    public static class JsonWebAlgorithmsKeyTypes {
 {
-        public const string EllipticCurve = "EC";

-        public const string Octet = "oct";

-        public const string RSA = "RSA";

-    }
-    public class JsonWebKey : SecurityKey {
 {
-        public JsonWebKey();

-        public JsonWebKey(string json);

-        public virtual IDictionary<string, object> AdditionalData { get; }

-        public string Alg { get; set; }

-        public string Crv { get; set; }

-        public string D { get; set; }

-        public string DP { get; set; }

-        public string DQ { get; set; }

-        public string E { get; set; }

-        public bool HasPrivateKey { get; }

-        public string K { get; set; }

-        public override string KeyId { get; set; }

-        public IList<string> KeyOps { get; private set; }

-        public override int KeySize { get; }

-        public string Kid { get; set; }

-        public string Kty { get; set; }

-        public string N { get; set; }

-        public IList<string> Oth { get; set; }

-        public string P { get; set; }

-        public string Q { get; set; }

-        public string QI { get; set; }

-        public string Use { get; set; }

-        public string X { get; set; }

-        public IList<string> X5c { get; private set; }

-        public string X5t { get; set; }

-        public string X5tS256 { get; set; }

-        public string X5u { get; set; }

-        public string Y { get; set; }

-        public static JsonWebKey Create(string json);

-        public bool ShouldSerializeKeyOps();

-        public bool ShouldSerializeX5c();

-    }
-    public class JsonWebKeyConverter {
 {
-        public JsonWebKeyConverter();

-        public static JsonWebKey ConvertFromRSASecurityKey(RsaSecurityKey key);

-        public static JsonWebKey ConvertFromSecurityKey(SecurityKey key);

-        public static JsonWebKey ConvertFromSymmetricSecurityKey(SymmetricSecurityKey key);

-        public static JsonWebKey ConvertFromX509SecurityKey(X509SecurityKey key);

-    }
-    public static class JsonWebKeyECTypes {
 {
-        public const string P256 = "P-256";

-        public const string P384 = "P-384";

-        public const string P512 = "P-512";

-        public const string P521 = "P-521";

-    }
-    public static class JsonWebKeyParameterNames {
 {
-        public const string Alg = "alg";

-        public const string Crv = "crv";

-        public const string D = "d";

-        public const string DP = "dp";

-        public const string DQ = "dq";

-        public const string E = "e";

-        public const string K = "k";

-        public const string KeyOps = "key_ops";

-        public const string Keys = "keys";

-        public const string Kid = "kid";

-        public const string Kty = "kty";

-        public const string N = "n";

-        public const string Oth = "oth";

-        public const string P = "p";

-        public const string Q = "q";

-        public const string QI = "qi";

-        public const string R = "r";

-        public const string T = "t";

-        public const string Use = "use";

-        public const string X = "x";

-        public const string X5c = "x5c";

-        public const string X5t = "x5t";

-        public const string X5tS256 = "x5t#S256";

-        public const string X5u = "x5u";

-        public const string Y = "y";

-    }
-    public class JsonWebKeySet {
 {
-        public JsonWebKeySet();

-        public JsonWebKeySet(string json);

-        public JsonWebKeySet(string json, JsonSerializerSettings jsonSerializerSettings);

-        public virtual IDictionary<string, object> AdditionalData { get; }

-        public IList<JsonWebKey> Keys { get; private set; }

-        public static JsonWebKeySet Create(string json);

-        public IList<SecurityKey> GetSigningKeys();

-    }
-    public static class JsonWebKeySetParameterNames {
 {
-        public const string Keys = "keys";

-    }
-    public static class JsonWebKeyUseNames {
 {
-        public const string Enc = "enc";

-        public const string Sig = "sig";

-    }
-    public abstract class KeyWrapProvider : IDisposable {
 {
-        protected KeyWrapProvider();

-        public abstract string Algorithm { get; }

-        public abstract string Context { get; set; }

-        public abstract SecurityKey Key { get; }

-        public void Dispose();

-        protected abstract void Dispose(bool disposing);

-        public abstract byte[] UnwrapKey(byte[] keyBytes);

-        public abstract byte[] WrapKey(byte[] keyBytes);

-    }
-    public delegate bool LifetimeValidator(Nullable<DateTime> notBefore, Nullable<DateTime> expires, SecurityToken securityToken, TokenValidationParameters validationParameters);

-    public enum PrivateKeyStatus {
 {
-        DoesNotExist = 1,

-        Exists = 0,

-        Unknown = 2,

-    }
-    public class RSACryptoServiceProviderProxy : RSA {
 {
-        public RSACryptoServiceProviderProxy(RSACryptoServiceProvider rsa);

-        public override string KeyExchangeAlgorithm { get; }

-        public override string SignatureAlgorithm { get; }

-        public byte[] Decrypt(byte[] input, bool fOAEP);

-        public override byte[] DecryptValue(byte[] input);

-        protected override void Dispose(bool disposing);

-        public byte[] Encrypt(byte[] input, bool fOAEP);

-        public override byte[] EncryptValue(byte[] input);

-        public override RSAParameters ExportParameters(bool includePrivateParameters);

-        public override void ImportParameters(RSAParameters parameters);

-        public byte[] SignData(byte[] input, object hash);

-        public bool VerifyData(byte[] input, object hash, byte[] signature);

-    }
-    public class RsaKeyWrapProvider : KeyWrapProvider {
 {
-        public RsaKeyWrapProvider(SecurityKey key, string algorithm, bool willUnwrap);

-        public override string Algorithm { get; }

-        public override string Context { get; set; }

-        public override SecurityKey Key { get; }

-        protected override void Dispose(bool disposing);

-        protected virtual bool IsSupportedAlgorithm(SecurityKey key, string algorithm);

-        public override byte[] UnwrapKey(byte[] keyBytes);

-        public override byte[] WrapKey(byte[] keyBytes);

-    }
-    public class RsaSecurityKey : AsymmetricSecurityKey {
 {
-        public RsaSecurityKey(RSA rsa);

-        public RsaSecurityKey(RSAParameters rsaParameters);

-        public override bool HasPrivateKey { get; }

-        public override int KeySize { get; }

-        public RSAParameters Parameters { get; private set; }

-        public override PrivateKeyStatus PrivateKeyStatus { get; }

-        public RSA Rsa { get; private set; }

-    }
-    public static class SecurityAlgorithms {
 {
-        public const string Aes128CbcHmacSha256 = "A128CBC-HS256";

-        public const string Aes128Encryption = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";

-        public const string Aes128KeyWrap = "http://www.w3.org/2001/04/xmlenc#kw-aes128";

-        public const string Aes128KW = "A128KW";

-        public const string Aes192CbcHmacSha384 = "A192CBC-HS384";

-        public const string Aes192Encryption = "http://www.w3.org/2001/04/xmlenc#aes192-cbc";

-        public const string Aes192KeyWrap = "http://www.w3.org/2001/04/xmlenc#kw-aes192";

-        public const string Aes256CbcHmacSha512 = "A256CBC-HS512";

-        public const string Aes256Encryption = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";

-        public const string Aes256KeyWrap = "http://www.w3.org/2001/04/xmlenc#kw-aes256";

-        public const string Aes256KW = "A256KW";

-        public const string DesEncryption = "http://www.w3.org/2001/04/xmlenc#des-cbc";

-        public const string EcdsaSha256 = "ES256";

-        public const string EcdsaSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";

-        public const string EcdsaSha384 = "ES384";

-        public const string EcdsaSha384Signature = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha384";

-        public const string EcdsaSha512 = "ES512";

-        public const string EcdsaSha512Signature = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512";

-        public const string EnvelopedSignature = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

-        public const string ExclusiveC14n = "http://www.w3.org/2001/10/xml-exc-c14n#";

-        public const string ExclusiveC14nWithComments = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";

-        public const string HmacSha256 = "HS256";

-        public const string HmacSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

-        public const string HmacSha384 = "HS384";

-        public const string HmacSha384Signature = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";

-        public const string HmacSha512 = "HS512";

-        public const string HmacSha512Signature = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";

-        public const string None = "none";

-        public const string Ripemd160Digest = "http://www.w3.org/2001/04/xmlenc#ripemd160";

-        public const string RsaOAEP = "RSA-OAEP";

-        public const string RsaOaepKeyWrap = "http://www.w3.org/2001/04/xmlenc#rsa-oaep";

-        public const string RsaPKCS1 = "RSA1_5";

-        public const string RsaSha256 = "RS256";

-        public const string RsaSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

-        public const string RsaSha384 = "RS384";

-        public const string RsaSha384Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";

-        public const string RsaSha512 = "RS512";

-        public const string RsaSha512Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";

-        public const string RsaSsaPssSha256 = "PS256";

-        public const string RsaSsaPssSha256Signature = "http://www.w3.org/2007/05/xmldsig-more#sha256-rsa-MGF1";

-        public const string RsaSsaPssSha384 = "PS384";

-        public const string RsaSsaPssSha384Signature = "http://www.w3.org/2007/05/xmldsig-more#sha384-rsa-MGF1";

-        public const string RsaSsaPssSha512 = "PS512";

-        public const string RsaSsaPssSha512Signature = "http://www.w3.org/2007/05/xmldsig-more#sha512-rsa-MGF1";

-        public const string RsaV15KeyWrap = "http://www.w3.org/2001/04/xmlenc#rsa-1_5";

-        public const string Sha256 = "SHA256";

-        public const string Sha256Digest = "http://www.w3.org/2001/04/xmlenc#sha256";

-        public const string Sha384 = "SHA384";

-        public const string Sha384Digest = "http://www.w3.org/2001/04/xmldsig-more#sha384";

-        public const string Sha512 = "SHA512";

-        public const string Sha512Digest = "http://www.w3.org/2001/04/xmlenc#sha512";

-    }
-    public abstract class SecurityKey {
 {
-        protected SecurityKey();

-        public CryptoProviderFactory CryptoProviderFactory { get; set; }

-        public virtual string KeyId { get; set; }

-        public abstract int KeySize { get; }

-    }
-    public class SecurityKeyIdentifierClause {
 {
-        public SecurityKeyIdentifierClause();

-    }
-    public abstract class SecurityToken {
 {
-        protected SecurityToken();

-        public abstract string Id { get; }

-        public abstract string Issuer { get; }

-        public abstract SecurityKey SecurityKey { get; }

-        public abstract SecurityKey SigningKey { get; set; }

-        public abstract DateTime ValidFrom { get; }

-        public abstract DateTime ValidTo { get; }

-    }
-    public class SecurityTokenCompressionFailedException : SecurityTokenException {
 {
-        public SecurityTokenCompressionFailedException();

-        public SecurityTokenCompressionFailedException(string message);

-        public SecurityTokenCompressionFailedException(string message, Exception inner);

-    }
-    public class SecurityTokenDecompressionFailedException : SecurityTokenException {
 {
-        public SecurityTokenDecompressionFailedException();

-        public SecurityTokenDecompressionFailedException(string message);

-        public SecurityTokenDecompressionFailedException(string message, Exception inner);

-    }
-    public class SecurityTokenDecryptionFailedException : SecurityTokenException {
 {
-        public SecurityTokenDecryptionFailedException();

-        public SecurityTokenDecryptionFailedException(string message);

-        public SecurityTokenDecryptionFailedException(string message, Exception innerException);

-    }
-    public class SecurityTokenDescriptor {
 {
-        public SecurityTokenDescriptor();

-        public string Audience { get; set; }

-        public EncryptingCredentials EncryptingCredentials { get; set; }

-        public Nullable<DateTime> Expires { get; set; }

-        public Nullable<DateTime> IssuedAt { get; set; }

-        public string Issuer { get; set; }

-        public Nullable<DateTime> NotBefore { get; set; }

-        public SigningCredentials SigningCredentials { get; set; }

-        public ClaimsIdentity Subject { get; set; }

-    }
-    public class SecurityTokenEncryptionFailedException : SecurityTokenException {
 {
-        public SecurityTokenEncryptionFailedException();

-        public SecurityTokenEncryptionFailedException(string message);

-        public SecurityTokenEncryptionFailedException(string message, Exception innerException);

-    }
-    public class SecurityTokenEncryptionKeyNotFoundException : SecurityTokenDecryptionFailedException {
 {
-        public SecurityTokenEncryptionKeyNotFoundException();

-        public SecurityTokenEncryptionKeyNotFoundException(string message);

-        public SecurityTokenEncryptionKeyNotFoundException(string message, Exception innerException);

-    }
-    public class SecurityTokenException : Exception {
 {
-        public SecurityTokenException();

-        public SecurityTokenException(string message);

-        public SecurityTokenException(string message, Exception innerException);

-    }
-    public class SecurityTokenExpiredException : SecurityTokenValidationException {
 {
-        public SecurityTokenExpiredException();

-        public SecurityTokenExpiredException(string message);

-        public SecurityTokenExpiredException(string message, Exception inner);

-        public DateTime Expires { get; set; }

-    }
-    public abstract class SecurityTokenHandler : TokenHandler, ISecurityTokenValidator {
 {
-        protected SecurityTokenHandler();

-        public virtual bool CanValidateToken { get; }

-        public virtual bool CanWriteToken { get; }

-        public abstract Type TokenType { get; }

-        public virtual bool CanReadToken(string tokenString);

-        public virtual SecurityKeyIdentifierClause CreateSecurityTokenReference(SecurityToken token, bool attached);

-        public virtual SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor);

-        public virtual SecurityToken ReadToken(string tokenString);

-        public virtual SecurityToken ReadToken(XmlReader reader);

-        public abstract SecurityToken ReadToken(XmlReader reader, TokenValidationParameters validationParameters);

-        public virtual ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken);

-        public virtual string WriteToken(SecurityToken token);

-        public abstract void WriteToken(XmlWriter writer, SecurityToken token);

-    }
-    public class SecurityTokenInvalidAudienceException : SecurityTokenValidationException {
 {
-        public SecurityTokenInvalidAudienceException();

-        public SecurityTokenInvalidAudienceException(string message);

-        public SecurityTokenInvalidAudienceException(string message, Exception innerException);

-        public string InvalidAudience { get; set; }

-    }
-    public class SecurityTokenInvalidIssuerException : SecurityTokenValidationException {
 {
-        public SecurityTokenInvalidIssuerException();

-        public SecurityTokenInvalidIssuerException(string message);

-        public SecurityTokenInvalidIssuerException(string message, Exception innerException);

-        public string InvalidIssuer { get; set; }

-    }
-    public class SecurityTokenInvalidLifetimeException : SecurityTokenValidationException {
 {
-        public SecurityTokenInvalidLifetimeException();

-        public SecurityTokenInvalidLifetimeException(string message);

-        public SecurityTokenInvalidLifetimeException(string message, Exception innerException);

-        public Nullable<DateTime> Expires { get; set; }

-        public Nullable<DateTime> NotBefore { get; set; }

-    }
-    public class SecurityTokenInvalidSignatureException : SecurityTokenValidationException {
 {
-        public SecurityTokenInvalidSignatureException();

-        public SecurityTokenInvalidSignatureException(string message);

-        public SecurityTokenInvalidSignatureException(string message, Exception innerException);

-    }
-    public class SecurityTokenInvalidSigningKeyException : SecurityTokenValidationException {
 {
-        public SecurityTokenInvalidSigningKeyException();

-        public SecurityTokenInvalidSigningKeyException(string message);

-        public SecurityTokenInvalidSigningKeyException(string message, Exception inner);

-        public SecurityKey SigningKey { get; set; }

-    }
-    public class SecurityTokenKeyWrapException : SecurityTokenException {
 {
-        public SecurityTokenKeyWrapException();

-        public SecurityTokenKeyWrapException(string message);

-        public SecurityTokenKeyWrapException(string message, Exception innerException);

-    }
-    public class SecurityTokenNoExpirationException : SecurityTokenValidationException {
 {
-        public SecurityTokenNoExpirationException();

-        public SecurityTokenNoExpirationException(string message);

-        public SecurityTokenNoExpirationException(string message, Exception innerException);

-    }
-    public class SecurityTokenNotYetValidException : SecurityTokenValidationException {
 {
-        public SecurityTokenNotYetValidException();

-        public SecurityTokenNotYetValidException(string message);

-        public SecurityTokenNotYetValidException(string message, Exception inner);

-        public DateTime NotBefore { get; set; }

-    }
-    public class SecurityTokenReplayAddFailedException : SecurityTokenValidationException {
 {
-        public SecurityTokenReplayAddFailedException();

-        public SecurityTokenReplayAddFailedException(string message);

-        public SecurityTokenReplayAddFailedException(string message, Exception innerException);

-    }
-    public class SecurityTokenReplayDetectedException : SecurityTokenValidationException {
 {
-        public SecurityTokenReplayDetectedException();

-        public SecurityTokenReplayDetectedException(string message);

-        public SecurityTokenReplayDetectedException(string message, Exception inner);

-    }
-    public class SecurityTokenSignatureKeyNotFoundException : SecurityTokenInvalidSignatureException {
 {
-        public SecurityTokenSignatureKeyNotFoundException();

-        public SecurityTokenSignatureKeyNotFoundException(string message);

-        public SecurityTokenSignatureKeyNotFoundException(string message, Exception innerException);

-    }
-    public class SecurityTokenValidationException : SecurityTokenException {
 {
-        public SecurityTokenValidationException();

-        public SecurityTokenValidationException(string message);

-        public SecurityTokenValidationException(string message, Exception innerException);

-    }
-    public abstract class SignatureProvider : IDisposable {
 {
-        protected SignatureProvider(SecurityKey key, string algorithm);

-        public string Algorithm { get; private set; }

-        public string Context { get; set; }

-        public CryptoProviderCache CryptoProviderCache { get; set; }

-        public SecurityKey Key { get; private set; }

-        public bool WillCreateSignatures { get; protected set; }

-        public void Dispose();

-        protected abstract void Dispose(bool disposing);

-        public abstract byte[] Sign(byte[] input);

-        public abstract bool Verify(byte[] input, byte[] signature);

-    }
-    public delegate SecurityToken SignatureValidator(string token, TokenValidationParameters validationParameters);

-    public class SigningCredentials {
 {
-        public SigningCredentials(SecurityKey key, string algorithm);

-        public SigningCredentials(SecurityKey key, string algorithm, string digest);

-        protected SigningCredentials(X509Certificate2 certificate);

-        protected SigningCredentials(X509Certificate2 certificate, string algorithm);

-        public string Algorithm { get; private set; }

-        public CryptoProviderFactory CryptoProviderFactory { get; set; }

-        public string Digest { get; private set; }

-        public SecurityKey Key { get; private set; }

-        public string Kid { get; }

-    }
-    public class SymmetricKeyWrapProvider : KeyWrapProvider {
 {
-        public SymmetricKeyWrapProvider(SecurityKey key, string algorithm);

-        public override string Algorithm { get; }

-        public override string Context { get; set; }

-        public override SecurityKey Key { get; }

-        protected override void Dispose(bool disposing);

-        protected virtual SymmetricAlgorithm GetSymmetricAlgorithm(SecurityKey key, string algorithm);

-        protected virtual bool IsSupportedAlgorithm(SecurityKey key, string algorithm);

-        public override byte[] UnwrapKey(byte[] keyBytes);

-        public override byte[] WrapKey(byte[] keyBytes);

-    }
-    public class SymmetricSecurityKey : SecurityKey {
 {
-        public SymmetricSecurityKey(byte[] key);

-        public virtual byte[] Key { get; }

-        public override int KeySize { get; }

-    }
-    public class SymmetricSignatureProvider : SignatureProvider {
 {
-        public static readonly int DefaultMinimumSymmetricKeySizeInBits;

-        public SymmetricSignatureProvider(SecurityKey key, string algorithm);

-        public SymmetricSignatureProvider(SecurityKey key, string algorithm, bool willCreateSignatures);

-        public int MinimumSymmetricKeySizeInBits { get; set; }

-        protected override void Dispose(bool disposing);

-        protected virtual byte[] GetKeyBytes(SecurityKey key);

-        protected virtual KeyedHashAlgorithm GetKeyedHashAlgorithm(byte[] keyBytes, string algorithm);

-        public override byte[] Sign(byte[] input);

-        public override bool Verify(byte[] input, byte[] signature);

-        public bool Verify(byte[] input, byte[] signature, int length);

-    }
-    public delegate IEnumerable<SecurityKey> TokenDecryptionKeyResolver(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters);

-    public abstract class TokenHandler {
 {
-        public static readonly int DefaultTokenLifetimeInMinutes;

-        protected TokenHandler();

-        public virtual int MaximumTokenSizeInBytes { get; set; }

-        public bool SetDefaultTimesOnTokenCreation { get; set; }

-        public int TokenLifetimeInMinutes { get; set; }

-    }
-    public delegate SecurityToken TokenReader(string token, TokenValidationParameters validationParameters);

-    public delegate bool TokenReplayValidator(Nullable<DateTime> expirationTime, string securityToken, TokenValidationParameters validationParameters);

-    public class TokenValidationParameters {
 {
-        public const int DefaultMaximumTokenSizeInBytes = 2097152;

-        public static readonly string DefaultAuthenticationType;

-        public static readonly TimeSpan DefaultClockSkew;

-        public TokenValidationParameters();

-        protected TokenValidationParameters(TokenValidationParameters other);

-        public TokenValidationParameters ActorValidationParameters { get; set; }

-        public AudienceValidator AudienceValidator { get; set; }

-        public string AuthenticationType { get; set; }

-        public TimeSpan ClockSkew { get; set; }

-        public CryptoProviderFactory CryptoProviderFactory { get; set; }

-        public SecurityKey IssuerSigningKey { get; set; }

-        public IssuerSigningKeyResolver IssuerSigningKeyResolver { get; set; }

-        public IEnumerable<SecurityKey> IssuerSigningKeys { get; set; }

-        public IssuerSigningKeyValidator IssuerSigningKeyValidator { get; set; }

-        public IssuerValidator IssuerValidator { get; set; }

-        public LifetimeValidator LifetimeValidator { get; set; }

-        public string NameClaimType { get; set; }

-        public Func<SecurityToken, string, string> NameClaimTypeRetriever { get; set; }

-        public IDictionary<string, object> PropertyBag { get; set; }

-        public bool RequireExpirationTime { get; set; }

-        public bool RequireSignedTokens { get; set; }

-        public string RoleClaimType { get; set; }

-        public Func<SecurityToken, string, string> RoleClaimTypeRetriever { get; set; }

-        public bool SaveSigninToken { get; set; }

-        public SignatureValidator SignatureValidator { get; set; }

-        public SecurityKey TokenDecryptionKey { get; set; }

-        public TokenDecryptionKeyResolver TokenDecryptionKeyResolver { get; set; }

-        public IEnumerable<SecurityKey> TokenDecryptionKeys { get; set; }

-        public TokenReader TokenReader { get; set; }

-        public ITokenReplayCache TokenReplayCache { get; set; }

-        public TokenReplayValidator TokenReplayValidator { get; set; }

-        public bool ValidateActor { get; set; }

-        public bool ValidateAudience { get; set; }

-        public bool ValidateIssuer { get; set; }

-        public bool ValidateIssuerSigningKey { get; set; }

-        public bool ValidateLifetime { get; set; }

-        public bool ValidateTokenReplay { get; set; }

-        public string ValidAudience { get; set; }

-        public IEnumerable<string> ValidAudiences { get; set; }

-        public string ValidIssuer { get; set; }

-        public IEnumerable<string> ValidIssuers { get; set; }

-        public virtual TokenValidationParameters Clone();

-        public virtual ClaimsIdentity CreateClaimsIdentity(SecurityToken securityToken, string issuer);

-    }
-    public static class UniqueId {
 {
-        public static string CreateRandomId();

-        public static string CreateRandomId(string prefix);

-        public static Uri CreateRandomUri();

-        public static string CreateUniqueId();

-        public static string CreateUniqueId(string prefix);

-    }
-    public static class Utility {
 {
-        public const string Empty = "empty";

-        public const string Null = "null";

-        public static bool AreEqual(byte[] a, byte[] b);

-        public static byte[] CloneByteArray(this byte[] src);

-        public static bool IsHttps(string address);

-        public static bool IsHttps(Uri uri);

-    }
-    public static class Validators {
 {
-        public static void ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        public static string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        public static void ValidateIssuerSecurityKey(SecurityKey securityKey, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        public static void ValidateLifetime(Nullable<DateTime> notBefore, Nullable<DateTime> expires, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        public static void ValidateTokenReplay(Nullable<DateTime> expirationTime, string securityToken, TokenValidationParameters validationParameters);

-        public static void ValidateTokenReplay(string securityToken, Nullable<DateTime> expirationTime, TokenValidationParameters validationParameters);

-    }
-    public class X509EncryptingCredentials : EncryptingCredentials {
 {
-        public X509EncryptingCredentials(X509Certificate2 certificate);

-        public X509EncryptingCredentials(X509Certificate2 certificate, string keyWrapAlgorithm, string dataEncryptionAlgorithm);

-        public X509Certificate2 Certificate { get; private set; }

-    }
-    public class X509SecurityKey : AsymmetricSecurityKey {
 {
-        public X509SecurityKey(X509Certificate2 certificate);

-        public X509Certificate2 Certificate { get; }

-        public override bool HasPrivateKey { get; }

-        public override int KeySize { get; }

-        public AsymmetricAlgorithm PrivateKey { get; }

-        public override PrivateKeyStatus PrivateKeyStatus { get; }

-        public AsymmetricAlgorithm PublicKey { get; }

-        public string X5t { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class X509SigningCredentials : SigningCredentials {
 {
-        public X509SigningCredentials(X509Certificate2 certificate);

-        public X509SigningCredentials(X509Certificate2 certificate, string algorithm);

-        public X509Certificate2 Certificate { get; private set; }

-    }
-}
```

