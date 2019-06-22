# Microsoft.IdentityModel.Tokens.Saml

``` diff
-namespace Microsoft.IdentityModel.Tokens.Saml {
 {
-    public class AuthenticationInformation {
 {
-        public AuthenticationInformation(Uri authenticationMethod, DateTime authenticationInstant);

-        public DateTime AuthenticationInstant { get; set; }

-        public Uri AuthenticationMethod { get; private set; }

-        public ICollection<SamlAuthorityBinding> AuthorityBindings { get; }

-        public string DnsName { get; set; }

-        public string IPAddress { get; set; }

-        public Nullable<DateTime> NotOnOrAfter { get; set; }

-        public string Session { get; set; }

-    }
-    public static class ClaimProperties {
 {
-        public const string Namespace = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties";

-        public const string SamlNameIdentifierFormat = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/format";

-        public const string SamlNameIdentifierNameQualifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/namequalifier";

-        public const string SamlNameIdentifierSPNameQualifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/spnamequalifier";

-        public const string SamlNameIdentifierSPProvidedId = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/spprovidedid";

-        public const string SamlSubjectConfirmationData = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/confirmationdata";

-        public const string SamlSubjectConfirmationMethod = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/confirmationmethod";

-        public const string SamlSubjectKeyInfo = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/keyinfo";

-    }
-    public class SamlAction {
 {
-        public SamlAction(string value);

-        public SamlAction(string value, Uri @namespace);

-        public Uri Namespace { get; set; }

-        public string Value { get; set; }

-    }
-    public class SamlAdvice {
 {
-        public SamlAdvice();

-        public SamlAdvice(IEnumerable<SamlAssertion> assertions);

-        public SamlAdvice(IEnumerable<string> references);

-        public SamlAdvice(IEnumerable<string> references, IEnumerable<SamlAssertion> assertions);

-        public ICollection<string> AssertionIdReferences { get; }

-        public ICollection<SamlAssertion> Assertions { get; }

-    }
-    public class SamlAssertion {
 {
-        public SamlAssertion(string assertionId, string issuer, DateTime issueInstant, SamlConditions samlConditions, SamlAdvice samlAdvice, IEnumerable<SamlStatement> samlStatements);

-        public SamlAdvice Advice { get; set; }

-        public string AssertionId { get; set; }

-        public SamlConditions Conditions { get; set; }

-        public string InclusiveNamespacesPrefixList { get; set; }

-        public DateTime IssueInstant { get; set; }

-        public string Issuer { get; set; }

-        public string MajorVersion { get; }

-        public string MinorVersion { get; }

-        public Signature Signature { get; set; }

-        public SigningCredentials SigningCredentials { get; set; }

-        public IList<SamlStatement> Statements { get; }

-    }
-    public class SamlAttribute {
 {
-        public SamlAttribute(string ns, string name, IEnumerable<string> values);

-        public SamlAttribute(string ns, string name, string value);

-        public string AttributeValueXsiType { get; set; }

-        public string ClaimType { get; set; }

-        public string Name { get; set; }

-        public string Namespace { get; set; }

-        public string OriginalIssuer { get; set; }

-        public ICollection<string> Values { get; }

-    }
-    public class SamlAttributeKeyComparer : IEqualityComparer<SamlAttributeKeyComparer.AttributeKey> {
 {
-        public SamlAttributeKeyComparer();

-        public bool Equals(SamlAttributeKeyComparer.AttributeKey x, SamlAttributeKeyComparer.AttributeKey y);

-        public int GetHashCode(SamlAttributeKeyComparer.AttributeKey obj);

-        public class AttributeKey {
 {
-            public AttributeKey(SamlAttribute attribute);

-            public override int GetHashCode();

-        }
-    }
-    public class SamlAttributeStatement : SamlSubjectStatement {
 {
-        public SamlAttributeStatement(SamlSubject samlSubject, SamlAttribute attribute);

-        public SamlAttributeStatement(SamlSubject samlSubject, IEnumerable<SamlAttribute> attributes);

-        public ICollection<SamlAttribute> Attributes { get; }

-    }
-    public class SamlAudienceRestrictionCondition : SamlCondition {
 {
-        public SamlAudienceRestrictionCondition(IEnumerable<Uri> audiences);

-        public SamlAudienceRestrictionCondition(Uri audience);

-        public ICollection<Uri> Audiences { get; }

-    }
-    public class SamlAuthenticationStatement : SamlSubjectStatement {
 {
-        public SamlAuthenticationStatement(SamlSubject samlSubject, string authenticationMethod, DateTime authenticationInstant, string dnsAddress, string ipAddress, IEnumerable<SamlAuthorityBinding> authorityBindings);

-        public DateTime AuthenticationInstant { get; set; }

-        public string AuthenticationMethod { get; set; }

-        public ICollection<SamlAuthorityBinding> AuthorityBindings { get; }

-        public string DnsAddress { get; set; }

-        public string IPAddress { get; set; }

-    }
-    public class SamlAuthorityBinding {
 {
-        public SamlAuthorityBinding(XmlQualifiedName authorityKind, string binding, string location);

-        public XmlQualifiedName AuthorityKind { get; set; }

-        public string Binding { get; set; }

-        public string Location { get; set; }

-    }
-    public class SamlAuthorizationDecisionStatement : SamlSubjectStatement {
 {
-        public SamlAuthorizationDecisionStatement(SamlSubject subject, string resource, string decision, IEnumerable<SamlAction> actions);

-        public SamlAuthorizationDecisionStatement(SamlSubject subject, string resource, string decision, IEnumerable<SamlAction> actions, SamlEvidence evidence);

-        public ICollection<SamlAction> Actions { get; }

-        public static string ClaimType { get; }

-        public string Decision { get; set; }

-        public SamlEvidence Evidence { get; set; }

-        public string Resource { get; set; }

-    }
-    public abstract class SamlCondition {
 {
-        protected SamlCondition();

-    }
-    public class SamlConditions {
 {
-        public SamlConditions(DateTime notBefore, DateTime notOnOrAfter);

-        public SamlConditions(DateTime notBefore, DateTime notOnOrAfter, IEnumerable<SamlCondition> conditions);

-        public ICollection<SamlCondition> Conditions { get; }

-        public DateTime NotBefore { get; set; }

-        public DateTime NotOnOrAfter { get; set; }

-    }
-    public static class SamlConstants {
 {
-        public const string AssertionIdPrefix = "SamlSecurityToken-";

-        public const string BearerConfirmationMethod = "urn:oasis:names:tc:SAML:1.0:cm:bearer";

-        public const string DefaultActionNamespace = "urn:oasis:names:tc:SAML:1.0:action:rwedc-negation";

-        public const string GeneratedDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

-        public const string MajorVersionValue = "1";

-        public const string MinorVersionValue = "1";

-        public const string Namespace = "urn:oasis:names:tc:SAML:1.0:assertion";

-        public const string NamespaceAttributePrefix = "NamespaceAttributePrefix";

-        public const string OasisWssSamlTokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";

-        public const string Prefix = "saml";

-        public const string Saml11Namespace = "urn:oasis:names:tc:SAML:1.0:assertion";

-        public const string Statement = "Statement";

-        public const string SubjectStatement = "SubjectStatement";

-        public const string UnspecifiedAuthenticationMethod = "urn:oasis:names:tc:SAML:1.0:am:unspecified";

-        public const string UserName = "UserName";

-        public const string UserNameNamespace = "urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName";

-        public static string[] AcceptedDateTimeFormats;

-        public static class AccessDecision {
 {
-            public static string Deny { get; }

-            public static string Indeterminate { get; }

-            public static string Permit { get; }

-        }
-        public static class AuthenticationMethods {
 {
-            public const string HardwareTokenString = "URI:urn:oasis:names:tc:SAML:1.0:am:HardwareToken";

-            public const string KerberosString = "urn:ietf:rfc:1510";

-            public const string PasswordString = "urn:oasis:names:tc:SAML:1.0:am:password";

-            public const string PgpString = "urn:oasis:names:tc:SAML:1.0:am:PGP";

-            public const string SecureRemotePasswordString = "urn:ietf:rfc:2945";

-            public const string SignatureString = "urn:ietf:rfc:3075";

-            public const string SpkiString = "urn:oasis:names:tc:SAML:1.0:am:SPKI";

-            public const string TlsClientString = "urn:ietf:rfc:2246";

-            public const string UnspecifiedString = "urn:oasis:names:tc:SAML:1.0:am:unspecified";

-            public const string WindowsString = "urn:federation:authentication:windows";

-            public const string X509String = "urn:oasis:names:tc:SAML:1.0:am:X509-PKI";

-            public const string XkmsString = "urn:oasis:names:tc:SAML:1.0:am:XKMS";

-        }
-        public static class Types {
 {
-            public const string ActionType = "ActionType";

-            public const string AdviceType = "AdviceType";

-            public const string AssertionType = "AssertionType";

-            public const string AttributeStatementType = "AttributeStatementType";

-            public const string AttributeType = "AttributeType";

-            public const string AudienceRestrictionType = "AudienceRestrictionType";

-            public const string AuthnContextType = "AuthnContextType";

-            public const string AuthnStatementType = "AuthnStatementType";

-            public const string AuthorityBindingType = "AuthorityBindingType";

-            public const string AuthzDecisionStatementType = "AuthzDecisionStatementType";

-            public const string BaseIDAbstractType = "BaseIDAbstractType";

-            public const string ConditionAbstractType = "ConditionAbstractType";

-            public const string ConditionsType = "ConditionsType";

-            public const string DoNotCacheConditionType = "DoNotCacheConditionType";

-            public const string EncryptedElementType = "EncryptedElementType";

-            public const string EvidenceType = "EvidenceType";

-            public const string KeyInfoConfirmationDataType = "KeyInfoConfirmationDataType";

-            public const string NameIDType = "NameIDType";

-            public const string OneTimeUseType = "OneTimeUseType";

-            public const string ProxyRestrictionType = "ProxyRestrictionType";

-            public const string StatementAbstractType = "StatementAbstractType";

-            public const string SubjectConfirmationDataType = "SubjectConfirmationDataType";

-            public const string SubjectConfirmationType = "SubjectConfirmationType";

-            public const string SubjectLocalityType = "SubjectLocalityType";

-            public const string SubjectType = "SubjectType";

-        }
-    }
-    public class SamlDoNotCacheCondition : SamlCondition {
 {
-        public SamlDoNotCacheCondition();

-    }
-    public class SamlEvidence {
 {
-        public SamlEvidence(IEnumerable<SamlAssertion> assertions);

-        public SamlEvidence(IEnumerable<string> assertionIDReferences);

-        public SamlEvidence(IEnumerable<string> assertionIDReferences, IEnumerable<SamlAssertion> assertions);

-        public ICollection<string> AssertionIDReferences { get; }

-        public ICollection<SamlAssertion> Assertions { get; }

-    }
-    public class SamlSecurityToken : SecurityToken {
 {
-        protected SamlSecurityToken();

-        public SamlSecurityToken(SamlAssertion assertion);

-        public SamlAssertion Assertion { get; }

-        public override string Id { get; }

-        public override string Issuer { get; }

-        public override SecurityKey SecurityKey { get; }

-        public override SecurityKey SigningKey { get; set; }

-        public override DateTime ValidFrom { get; }

-        public override DateTime ValidTo { get; }

-    }
-    public class SamlSecurityTokenException : SecurityTokenException {
 {
-        public SamlSecurityTokenException();

-        public SamlSecurityTokenException(string message);

-        public SamlSecurityTokenException(string message, Exception innerException);

-    }
-    public class SamlSecurityTokenHandler : SecurityTokenHandler {
 {
-        public SamlSecurityTokenHandler();

-        public override bool CanValidateToken { get; }

-        public override bool CanWriteToken { get; }

-        public IEqualityComparer<SamlSubject> SamlSubjectEqualityComparer { get; set; }

-        public SamlSerializer Serializer { get; set; }

-        public override Type TokenType { get; }

-        protected virtual void AddActorToAttributes(ICollection<SamlAttribute> attributes, ClaimsIdentity subject);

-        public override bool CanReadToken(string securityToken);

-        public bool CanReadToken(XmlReader reader);

-        protected virtual ICollection<SamlAttribute> ConsolidateAttributes(ICollection<SamlAttribute> attributes);

-        protected virtual SamlAdvice CreateAdvice(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual SamlAttribute CreateAttribute(Claim claim);

-        protected virtual SamlAttributeStatement CreateAttributeStatement(SamlSubject subject, SecurityTokenDescriptor tokenDescriptor);

-        protected virtual SamlAuthenticationStatement CreateAuthenticationStatement(SamlSubject subject, AuthenticationInformation authenticationInformation);

-        public virtual SamlAuthorizationDecisionStatement CreateAuthorizationDecisionStatement(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual IEnumerable<ClaimsIdentity> CreateClaimsIdentities(SamlSecurityToken samlToken, string issuer, TokenValidationParameters validationParameters);

-        protected virtual SamlConditions CreateConditions(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual ICollection<SamlStatement> CreateStatements(SecurityTokenDescriptor tokenDescriptor, AuthenticationInformation authenticationInformation);

-        protected virtual SamlSubject CreateSubject(SecurityTokenDescriptor tokenDescriptor);

-        public override SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor);

-        public virtual SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor, AuthenticationInformation authenticationInformation);

-        protected virtual string CreateXmlStringFromAttributes(ICollection<SamlAttribute> attributes);

-        protected virtual void ProcessAttributeStatement(SamlAttributeStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessAuthenticationStatement(SamlAuthenticationStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessAuthorizationDecisionStatement(SamlAuthorizationDecisionStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessCustomSubjectStatement(SamlStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual IEnumerable<ClaimsIdentity> ProcessStatements(SamlSecurityToken samlToken, string issuer, TokenValidationParameters validationParameters);

-        protected virtual void ProcessSubject(SamlSubject subject, ClaimsIdentity identity, string issuer);

-        public virtual SamlSecurityToken ReadSamlToken(string token);

-        public override SecurityToken ReadToken(string token);

-        public override SecurityToken ReadToken(XmlReader reader, TokenValidationParameters validationParameters);

-        protected virtual SecurityKey ResolveIssuerSigningKey(string token, SamlSecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void SetDelegateFromAttribute(SamlAttribute attribute, ClaimsIdentity subject, string issuer);

-        protected virtual void ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateConditions(SamlSecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateIssuerSecurityKey(SecurityKey key, SamlSecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateIssuerSecurityKey(SecurityKey securityKey, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateLifetime(Nullable<DateTime> notBefore, Nullable<DateTime> expires, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual SamlSecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters);

-        public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken);

-        protected virtual void ValidateTokenReplay(Nullable<DateTime> expiration, string token, TokenValidationParameters validationParameters);

-        public override string WriteToken(SecurityToken token);

-        public override void WriteToken(XmlWriter writer, SecurityToken token);

-    }
-    public class SamlSecurityTokenReadException : SamlSecurityTokenException {
 {
-        public SamlSecurityTokenReadException();

-        public SamlSecurityTokenReadException(string message);

-        public SamlSecurityTokenReadException(string message, Exception innerException);

-    }
-    public class SamlSecurityTokenWriteException : SamlSecurityTokenException {
 {
-        public SamlSecurityTokenWriteException();

-        public SamlSecurityTokenWriteException(string message);

-        public SamlSecurityTokenWriteException(string message, Exception innerException);

-    }
-    public class SamlSerializer {
 {
-        public SamlSerializer();

-        public DSigSerializer DSigSerializer { get; set; }

-        public string Prefix { get; set; }

-        protected virtual SamlAction ReadAction(XmlDictionaryReader reader);

-        protected virtual SamlAdvice ReadAdvice(XmlDictionaryReader reader);

-        public virtual SamlAssertion ReadAssertion(XmlReader reader);

-        public virtual SamlAttribute ReadAttribute(XmlDictionaryReader reader);

-        protected virtual SamlAttributeStatement ReadAttributeStatement(XmlDictionaryReader reader);

-        protected virtual SamlAudienceRestrictionCondition ReadAudienceRestrictionCondition(XmlDictionaryReader reader);

-        protected virtual SamlAuthenticationStatement ReadAuthenticationStatement(XmlDictionaryReader reader);

-        protected virtual SamlAuthorityBinding ReadAuthorityBinding(XmlDictionaryReader reader);

-        protected virtual SamlAuthorizationDecisionStatement ReadAuthorizationDecisionStatement(XmlDictionaryReader reader);

-        protected virtual SamlCondition ReadCondition(XmlDictionaryReader reader);

-        protected virtual SamlConditions ReadConditions(XmlDictionaryReader reader);

-        protected virtual SamlDoNotCacheCondition ReadDoNotCacheCondition(XmlDictionaryReader reader);

-        protected virtual SamlEvidence ReadEvidence(XmlDictionaryReader reader);

-        protected virtual SamlStatement ReadStatement(XmlDictionaryReader reader);

-        protected virtual SamlSubject ReadSubject(XmlDictionaryReader reader);

-        protected virtual void WriteAction(XmlWriter writer, SamlAction action);

-        protected virtual void WriteAdvice(XmlWriter writer, SamlAdvice advice);

-        public virtual void WriteAssertion(XmlWriter writer, SamlAssertion assertion);

-        public virtual void WriteAttribute(XmlWriter writer, SamlAttribute attribute);

-        protected virtual void WriteAttributeStatement(XmlWriter writer, SamlAttributeStatement statement);

-        protected virtual void WriteAudienceRestrictionCondition(XmlWriter writer, SamlAudienceRestrictionCondition audienceRestriction);

-        protected virtual void WriteAuthenticationStatement(XmlWriter writer, SamlAuthenticationStatement statement);

-        protected virtual void WriteAuthorityBinding(XmlWriter writer, SamlAuthorityBinding authorityBinding);

-        protected virtual void WriteAuthorizationDecisionStatement(XmlWriter writer, SamlAuthorizationDecisionStatement statement);

-        protected virtual void WriteCondition(XmlWriter writer, SamlCondition condition);

-        protected virtual void WriteConditions(XmlWriter writer, SamlConditions conditions);

-        protected virtual void WriteDoNotCacheCondition(XmlWriter writer, SamlDoNotCacheCondition condition);

-        protected virtual void WriteEvidence(XmlWriter writer, SamlEvidence evidence);

-        protected virtual void WriteStatement(XmlWriter writer, SamlStatement statement);

-        protected virtual void WriteSubject(XmlWriter writer, SamlSubject subject);

-    }
-    public abstract class SamlStatement {
 {
-        protected SamlStatement();

-    }
-    public class SamlSubject {
 {
-        public SamlSubject();

-        public SamlSubject(string nameFormat, string nameQualifier, string name);

-        public SamlSubject(string nameFormat, string nameQualifier, string name, IEnumerable<string> confirmations, string confirmationData);

-        public string ConfirmationData { get; set; }

-        public ICollection<string> ConfirmationMethods { get; }

-        public SecurityKey Key { get; set; }

-        public KeyInfo KeyInfo { get; set; }

-        public string Name { get; set; }

-        public static string NameClaimType { get; }

-        public string NameFormat { get; set; }

-        public string NameQualifier { get; set; }

-    }
-    public abstract class SamlSubjectStatement : SamlStatement {
 {
-        protected SamlSubjectStatement();

-        public virtual SamlSubject Subject { get; set; }

-    }
-}
```

