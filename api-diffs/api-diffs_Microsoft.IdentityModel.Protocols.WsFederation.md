# Microsoft.IdentityModel.Protocols.WsFederation

``` diff
-namespace Microsoft.IdentityModel.Protocols.WsFederation {
 {
-    public class SecurityTokenServiceTypeRoleDescriptor {
 {
-        public SecurityTokenServiceTypeRoleDescriptor();

-        public List<KeyInfo> KeyInfos { get; set; }

-        public string TokenEndpoint { get; set; }

-    }
-    public class WsFederationConfiguration {
 {
-        public WsFederationConfiguration();

-        public string Issuer { get; set; }

-        public ICollection<KeyInfo> KeyInfos { get; }

-        public Signature Signature { get; set; }

-        public SigningCredentials SigningCredentials { get; set; }

-        public ICollection<SecurityKey> SigningKeys { get; }

-        public string TokenEndpoint { get; set; }

-    }
-    public class WsFederationConfigurationRetriever : IConfigurationRetriever<WsFederationConfiguration> {
 {
-        public WsFederationConfigurationRetriever();

-        public static Task<WsFederationConfiguration> GetAsync(string address, IDocumentRetriever retriever, CancellationToken cancel);

-        public static Task<WsFederationConfiguration> GetAsync(string address, HttpClient httpClient, CancellationToken cancel);

-        public static Task<WsFederationConfiguration> GetAsync(string address, CancellationToken cancel);

-        Task<WsFederationConfiguration> Microsoft.IdentityModel.Protocols.IConfigurationRetriever<Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConfiguration>.GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel);

-    }
-    public static class WsFederationConstants {
 {
-        public const string MetadataNamespace = "urn:oasis:names:tc:SAML:2.0:metadata";

-        public const string Namespace = "http://docs.oasis-open.org/wsfed/federation/200706";

-        public const string PreferredPrefix = "fed";

-        public static class Attributes {
 {
-            public const string EntityId = "entityID";

-            public const string Id = "ID";

-            public const string ProtocolSupportEnumeration = "protocolSupportEnumeration";

-            public const string Type = "type";

-            public const string Use = "use";

-        }
-        public static class Elements {
 {
-            public const string EntityDescriptor = "EntityDescriptor";

-            public const string IdpssoDescriptor = "IDPSSODescriptor";

-            public const string KeyDescriptor = "KeyDescriptor";

-            public const string PassiveRequestorEndpoint = "PassiveRequestorEndpoint";

-            public const string RoleDescriptor = "RoleDescriptor";

-            public const string SpssoDescriptor = "SPSSODescriptor";

-        }
-        public static class KeyUse {
 {
-            public const string Signing = "signing";

-        }
-        public static class Namespaces

-        public static class Types {
 {
-            public const string ApplicationServiceType = "ApplicationServiceType";

-            public const string SecurityTokenServiceType = "SecurityTokenServiceType";

-        }
-        public static class WsFederationActions {
 {
-            public const string Attribute = "wattr1.0";

-            public const string Pseudonym = "wpseudo1.0";

-            public const string SignIn = "wsignin1.0";

-            public const string SignOut = "wsignout1.0";

-            public const string SignOutCleanup = "wsignoutcleanup1.0";

-        }
-        public static class WsFederationFaultCodes {
 {
-            public const string AlreadySignedIn = "AlreadySignedIn";

-            public const string BadRequest = "BadRequest";

-            public const string IssuerNameNotSupported = "IssuerNameNotSupported";

-            public const string NeedFresherCredentials = "NeedFresherCredentials";

-            public const string NoMatchInScope = "NoMatchInScope";

-            public const string NoPseudonymInScope = "NoPseudonymInScope";

-            public const string NotSignedIn = "NotSignedIn";

-            public const string RstParameterNotAccepted = "RstParameterNotAccepted";

-            public const string SpecificPolicy = "SpecificPolicy";

-            public const string UnsupportedClaimsDialect = "UnsupportedClaimsDialect";

-            public const string UnsupportedEncoding = "UnsupportedEncoding";

-        }
-        public static class WsFederationParameterNames {
 {
-            public const string Wa = "wa";

-            public const string Wattr = "wattr";

-            public const string Wattrptr = "wattrptr";

-            public const string Wauth = "wauth";

-            public const string Wct = "wct";

-            public const string Wctx = "wctx";

-            public const string Wencoding = "wencoding";

-            public const string Wfed = "wfed";

-            public const string Wfresh = "wfresh";

-            public const string Whr = "whr";

-            public const string Wp = "wp";

-            public const string Wpseudo = "wpseudo";

-            public const string Wpseudoptr = "wpseudoptr";

-            public const string Wreply = "wreply";

-            public const string Wreq = "wreq";

-            public const string Wreqptr = "wreqptr";

-            public const string Wres = "wres";

-            public const string Wresult = "wresult";

-            public const string Wresultptr = "wresultptr";

-            public const string Wtrealm = "wtrealm";

-        }
-    }
-    public class WsFederationException : Exception {
 {
-        public WsFederationException();

-        public WsFederationException(string message);

-        public WsFederationException(string message, Exception innerException);

-    }
-    public class WsFederationMessage : AuthenticationProtocolMessage {
 {
-        public WsFederationMessage();

-        public WsFederationMessage(WsFederationMessage wsFederationMessage);

-        public WsFederationMessage(IEnumerable<KeyValuePair<string, string[]>> parameters);

-        public bool IsSignInMessage { get; }

-        public bool IsSignOutMessage { get; }

-        public string Wa { get; set; }

-        public string Wattr { get; set; }

-        public string Wattrptr { get; set; }

-        public string Wauth { get; set; }

-        public string Wct { get; set; }

-        public string Wctx { get; set; }

-        public string Wencoding { get; set; }

-        public string Wfed { get; set; }

-        public string Wfresh { get; set; }

-        public string Whr { get; set; }

-        public string Wp { get; set; }

-        public string Wpseudo { get; set; }

-        public string Wpseudoptr { get; set; }

-        public string Wreply { get; set; }

-        public string Wreq { get; set; }

-        public string Wreqptr { get; set; }

-        public string Wres { get; set; }

-        public string Wresult { get; set; }

-        public string Wresultptr { get; set; }

-        public string Wtrealm { get; set; }

-        public string CreateSignInUrl();

-        public string CreateSignOutUrl();

-        public static WsFederationMessage FromQueryString(string queryString);

-        public static WsFederationMessage FromUri(Uri uri);

-        public virtual string GetToken();

-        public virtual string GetTokenUsingXmlReader();

-    }
-    public class WsFederationMetadataSerializer {
 {
-        public WsFederationMetadataSerializer();

-        public string PreferredPrefix { get; set; }

-        protected virtual WsFederationConfiguration ReadEntityDescriptor(XmlReader reader);

-        protected virtual KeyInfo ReadKeyDescriptorForSigning(XmlReader reader);

-        public WsFederationConfiguration ReadMetadata(XmlReader reader);

-        protected virtual string ReadPassiveRequestorEndpoint(XmlReader reader);

-        protected virtual SecurityTokenServiceTypeRoleDescriptor ReadSecurityTokenServiceTypeRoleDescriptor(XmlReader reader);

-        public void WriteMetadata(XmlWriter writer, WsFederationConfiguration configuration);

-    }
-    public class WsFederationReadException : WsFederationException {
 {
-        public WsFederationReadException();

-        public WsFederationReadException(string message);

-        public WsFederationReadException(string message, Exception innerException);

-    }
-}
```

