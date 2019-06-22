# System.IdentityModel.Tokens.Jwt

``` diff
-namespace System.IdentityModel.Tokens.Jwt {
 {
-    public delegate object Deserializer(string obj, Type targetType);

-    public static class JsonClaimValueTypes {
 {
-        public const string Json = "JSON";

-        public const string JsonArray = "JSON_ARRAY";

-        public const string JsonNull = "JSON_NULL";

-    }
-    public static class JsonExtensions {
 {
-        public static Deserializer Deserializer { get; set; }

-        public static Serializer Serializer { get; set; }

-        public static T DeserializeFromJson<T>(string jsonString) where T : class;

-        public static JwtHeader DeserializeJwtHeader(string jsonString);

-        public static JwtPayload DeserializeJwtPayload(string jsonString);

-        public static string SerializeToJson(object value);

-    }
-    public static class JwtConstants {
 {
-        public const string DirectKeyUseAlg = "dir";

-        public const string HeaderType = "JWT";

-        public const string HeaderTypeAlt = "http://openid.net/specs/jwt/1.0";

-        public const string JsonCompactSerializationRegex = "^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]*$";

-        public const string JweCompactSerializationRegex = "^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]*\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$";

-        public const string TokenType = "JWT";

-        public const string TokenTypeAlt = "urn:ietf:params:oauth:token-type:jwt";

-    }
-    public class JwtHeader : Dictionary<string, object> {
 {
-        public JwtHeader();

-        public JwtHeader(EncryptingCredentials encryptingCredentials);

-        public JwtHeader(EncryptingCredentials encryptingCredentials, IDictionary<string, string> outboundAlgorithmMap);

-        public JwtHeader(SigningCredentials signingCredentials);

-        public JwtHeader(SigningCredentials signingCredentials, IDictionary<string, string> outboundAlgorithmMap);

-        public string Alg { get; private set; }

-        public string Cty { get; private set; }

-        public string Enc { get; private set; }

-        public EncryptingCredentials EncryptingCredentials { get; private set; }

-        public string IV { get; }

-        public string Kid { get; private set; }

-        public SigningCredentials SigningCredentials { get; private set; }

-        public string Typ { get; private set; }

-        public string X5t { get; }

-        public static JwtHeader Base64UrlDeserialize(string base64UrlEncodedJsonString);

-        public virtual string Base64UrlEncode();

-        public static JwtHeader Deserialize(string jsonString);

-        public virtual string SerializeToJson();

-    }
-    public struct JwtHeaderParameterNames {
 {
-        public const string Alg = "alg";

-        public const string Cty = "cty";

-        public const string Enc = "enc";

-        public const string IV = "iv";

-        public const string Jku = "jku";

-        public const string Jwk = "jwk";

-        public const string Kid = "kid";

-        public const string Typ = "typ";

-        public const string X5c = "x5c";

-        public const string X5t = "x5t";

-        public const string X5u = "x5u";

-        public const string Zip = "zip";

-    }
-    public class JwtPayload : Dictionary<string, object> {
 {
-        public JwtPayload();

-        public JwtPayload(IEnumerable<Claim> claims);

-        public JwtPayload(string issuer, string audience, IEnumerable<Claim> claims, Nullable<DateTime> notBefore, Nullable<DateTime> expires);

-        public JwtPayload(string issuer, string audience, IEnumerable<Claim> claims, Nullable<DateTime> notBefore, Nullable<DateTime> expires, Nullable<DateTime> issuedAt);

-        public string Acr { get; }

-        public string Actort { get; }

-        public IList<string> Amr { get; }

-        public IList<string> Aud { get; }

-        public Nullable<int> AuthTime { get; }

-        public string Azp { get; }

-        public string CHash { get; }

-        public virtual IEnumerable<Claim> Claims { get; }

-        public Nullable<int> Exp { get; }

-        public Nullable<int> Iat { get; }

-        public string Iss { get; }

-        public string Jti { get; }

-        public Nullable<int> Nbf { get; }

-        public string Nonce { get; }

-        public string Sub { get; }

-        public DateTime ValidFrom { get; }

-        public DateTime ValidTo { get; }

-        public void AddClaim(Claim claim);

-        public void AddClaims(IEnumerable<Claim> claims);

-        public static JwtPayload Base64UrlDeserialize(string base64UrlEncodedJsonString);

-        public virtual string Base64UrlEncode();

-        public static JwtPayload Deserialize(string jsonString);

-        public virtual string SerializeToJson();

-    }
-    public struct JwtRegisteredClaimNames {
 {
-        public const string Acr = "acr";

-        public const string Actort = "actort";

-        public const string Amr = "amr";

-        public const string AtHash = "at_hash";

-        public const string Aud = "aud";

-        public const string AuthTime = "auth_time";

-        public const string Azp = "azp";

-        public const string Birthdate = "birthdate";

-        public const string CHash = "c_hash";

-        public const string Email = "email";

-        public const string Exp = "exp";

-        public const string FamilyName = "family_name";

-        public const string Gender = "gender";

-        public const string GivenName = "given_name";

-        public const string Iat = "iat";

-        public const string Iss = "iss";

-        public const string Jti = "jti";

-        public const string NameId = "nameid";

-        public const string Nbf = "nbf";

-        public const string Nonce = "nonce";

-        public const string Prn = "prn";

-        public const string Sid = "sid";

-        public const string Sub = "sub";

-        public const string Typ = "typ";

-        public const string UniqueName = "unique_name";

-        public const string Website = "website";

-    }
-    public class JwtSecurityToken : SecurityToken {
 {
-        public JwtSecurityToken(JwtHeader header, JwtPayload payload);

-        public JwtSecurityToken(JwtHeader header, JwtPayload payload, string rawHeader, string rawPayload, string rawSignature);

-        public JwtSecurityToken(JwtHeader header, JwtSecurityToken innerToken, string rawHeader, string rawEncryptedKey, string rawInitializationVector, string rawCiphertext, string rawAuthenticationTag);

-        public JwtSecurityToken(string jwtEncodedString);

-        public JwtSecurityToken(string issuer = null, string audience = null, IEnumerable<Claim> claims = null, Nullable<DateTime> notBefore = default(Nullable<DateTime>), Nullable<DateTime> expires = default(Nullable<DateTime>), SigningCredentials signingCredentials = null);

-        public string Actor { get; }

-        public IEnumerable<string> Audiences { get; }

-        public IEnumerable<Claim> Claims { get; }

-        public virtual string EncodedHeader { get; }

-        public virtual string EncodedPayload { get; }

-        public EncryptingCredentials EncryptingCredentials { get; }

-        public JwtHeader Header { get; internal set; }

-        public override string Id { get; }

-        public JwtSecurityToken InnerToken { get; internal set; }

-        public override string Issuer { get; }

-        public JwtPayload Payload { get; internal set; }

-        public string RawAuthenticationTag { get; private set; }

-        public string RawCiphertext { get; private set; }

-        public string RawData { get; private set; }

-        public string RawEncryptedKey { get; private set; }

-        public string RawHeader { get; internal set; }

-        public string RawInitializationVector { get; private set; }

-        public string RawPayload { get; internal set; }

-        public string RawSignature { get; internal set; }

-        public override SecurityKey SecurityKey { get; }

-        public string SignatureAlgorithm { get; }

-        public SigningCredentials SigningCredentials { get; }

-        public override SecurityKey SigningKey { get; set; }

-        public string Subject { get; }

-        public override DateTime ValidFrom { get; }

-        public override DateTime ValidTo { get; }

-        public override string ToString();

-    }
-    public class JwtSecurityTokenHandler : SecurityTokenHandler {
 {
-        public static bool DefaultMapInboundClaims;

-        public static IDictionary<string, string> DefaultInboundClaimTypeMap;

-        public static IDictionary<string, string> DefaultOutboundAlgorithmMap;

-        public static IDictionary<string, string> DefaultOutboundClaimTypeMap;

-        public static ISet<string> DefaultInboundClaimFilter;

-        public JwtSecurityTokenHandler();

-        public override bool CanValidateToken { get; }

-        public override bool CanWriteToken { get; }

-        public ISet<string> InboundClaimFilter { get; set; }

-        public IDictionary<string, string> InboundClaimTypeMap { get; set; }

-        public static string JsonClaimTypeProperty { get; set; }

-        public bool MapInboundClaims { get; set; }

-        public IDictionary<string, string> OutboundAlgorithmMap { get; }

-        public IDictionary<string, string> OutboundClaimTypeMap { get; set; }

-        public static string ShortClaimTypeProperty { get; set; }

-        public override Type TokenType { get; }

-        public override bool CanReadToken(string token);

-        protected virtual string CreateActorValue(ClaimsIdentity actor);

-        protected virtual ClaimsIdentity CreateClaimsIdentity(JwtSecurityToken jwtToken, string issuer, TokenValidationParameters validationParameters);

-        public virtual string CreateEncodedJwt(SecurityTokenDescriptor tokenDescriptor);

-        public virtual string CreateEncodedJwt(string issuer, string audience, ClaimsIdentity subject, Nullable<DateTime> notBefore, Nullable<DateTime> expires, Nullable<DateTime> issuedAt, SigningCredentials signingCredentials);

-        public virtual string CreateEncodedJwt(string issuer, string audience, ClaimsIdentity subject, Nullable<DateTime> notBefore, Nullable<DateTime> expires, Nullable<DateTime> issuedAt, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials);

-        public virtual JwtSecurityToken CreateJwtSecurityToken(SecurityTokenDescriptor tokenDescriptor);

-        public virtual JwtSecurityToken CreateJwtSecurityToken(string issuer = null, string audience = null, ClaimsIdentity subject = null, Nullable<DateTime> notBefore = default(Nullable<DateTime>), Nullable<DateTime> expires = default(Nullable<DateTime>), Nullable<DateTime> issuedAt = default(Nullable<DateTime>), SigningCredentials signingCredentials = null);

-        public virtual JwtSecurityToken CreateJwtSecurityToken(string issuer, string audience, ClaimsIdentity subject, Nullable<DateTime> notBefore, Nullable<DateTime> expires, Nullable<DateTime> issuedAt, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials);

-        public override SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor);

-        protected string DecryptToken(JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        public JwtSecurityToken ReadJwtToken(string token);

-        public override SecurityToken ReadToken(string token);

-        public override SecurityToken ReadToken(XmlReader reader, TokenValidationParameters validationParameters);

-        protected virtual SecurityKey ResolveIssuerSigningKey(string token, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual SecurityKey ResolveTokenDecryptionKey(string token, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateAudience(IEnumerable<string> audiences, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual string ValidateIssuer(string issuer, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateIssuerSecurityKey(SecurityKey key, JwtSecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateLifetime(Nullable<DateTime> notBefore, Nullable<DateTime> expires, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual JwtSecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters);

-        public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken);

-        protected ClaimsPrincipal ValidateTokenPayload(JwtSecurityToken jwtToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateTokenReplay(Nullable<DateTime> expires, string securityToken, TokenValidationParameters validationParameters);

-        public override string WriteToken(SecurityToken token);

-        public override void WriteToken(XmlWriter writer, SecurityToken token);

-    }
-    public delegate string Serializer(object obj);

-}
```

