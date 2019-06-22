# Microsoft.IdentityModel.JsonWebTokens

``` diff
-namespace Microsoft.IdentityModel.JsonWebTokens {
 {
-    public static class JsonClaimValueTypes {
 {
-        public const string Json = "JSON";

-        public const string JsonArray = "JSON_ARRAY";

-        public const string JsonNull = "JSON_NULL";

-    }
-    public class JsonWebToken : SecurityToken {
 {
-        public JsonWebToken(string jwtEncodedString);

-        public JsonWebToken(string header, string payload);

-        public string Actor { get; }

-        public string Alg { get; }

-        public IEnumerable<string> Audiences { get; }

-        public string AuthenticationTag { get; private set; }

-        public string Ciphertext { get; private set; }

-        public virtual IEnumerable<Claim> Claims { get; }

-        public string Cty { get; }

-        public string Enc { get; }

-        public string EncodedHeader { get; internal set; }

-        public string EncodedPayload { get; internal set; }

-        public string EncodedSignature { get; internal set; }

-        public string EncodedToken { get; private set; }

-        public string EncryptedKey { get; private set; }

-        public override string Id { get; }

-        public string InitializationVector { get; private set; }

-        public JsonWebToken InnerToken { get; internal set; }

-        public DateTime IssuedAt { get; }

-        public override string Issuer { get; }

-        public string Kid { get; }

-        public override SecurityKey SecurityKey { get; }

-        public override SecurityKey SigningKey { get; set; }

-        public string Subject { get; }

-        public string Typ { get; }

-        public override DateTime ValidFrom { get; }

-        public override DateTime ValidTo { get; }

-        public string X5t { get; }

-        public string Zip { get; }

-        public T GetHeaderValue<T>(string key);

-        public T GetPayloadValue<T>(string key);

-    }
-    public class JsonWebTokenHandler : TokenHandler {
 {
-        public JsonWebTokenHandler();

-        public bool CanValidateToken { get; }

-        public Type TokenType { get; }

-        public bool CanReadToken(string token);

-        public string CreateToken(string payload, SigningCredentials signingCredentials);

-        public string CreateToken(string payload, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials);

-        public string CreateToken(string payload, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials, string compressionAlgorithm);

-        protected string DecryptToken(JsonWebToken jwtToken, TokenValidationParameters validationParameters);

-        public JsonWebToken ReadJsonWebToken(string token);

-        public SecurityToken ReadToken(string token);

-        protected virtual SecurityKey ResolveTokenDecryptionKey(string token, JsonWebToken jwtToken, TokenValidationParameters validationParameters);

-        public TokenValidationResult ValidateToken(string token, TokenValidationParameters validationParameters);

-    }
-    public static class JwtConstants {
 {
-        public const int JweSegmentCount = 5;

-        public const int JwsSegmentCount = 3;

-        public const int MaxJwtSegmentCount = 5;

-        public const string DirectKeyUseAlg = "dir";

-        public const string HeaderType = "JWT";

-        public const string HeaderTypeAlt = "http://openid.net/specs/jwt/1.0";

-        public const string JsonCompactSerializationRegex = "^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]*$";

-        public const string JweCompactSerializationRegex = "^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]*\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$";

-        public const string TokenType = "JWT";

-        public const string TokenTypeAlt = "urn:ietf:params:oauth:token-type:jwt";

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
-    public class JwtTokenUtilities {
 {
-        public static Regex RegexJwe;

-        public static Regex RegexJws;

-        public JwtTokenUtilities();

-        public static string CreateEncodedSignature(string input, SigningCredentials signingCredentials);

-        public static byte[] GenerateKeyBytes(int sizeInBits);

-        public static IEnumerable<SecurityKey> GetAllDecryptionKeys(TokenValidationParameters validationParameters);

-    }
-    public class TokenValidationResult {
 {
-        public TokenValidationResult();

-        public SecurityToken SecurityToken { get; set; }

-    }
-}
```

