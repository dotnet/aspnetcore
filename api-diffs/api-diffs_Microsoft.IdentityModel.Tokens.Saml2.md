# Microsoft.IdentityModel.Tokens.Saml2

``` diff
-namespace Microsoft.IdentityModel.Tokens.Saml2 {
 {
-    public class AuthenticationInformation {
 {
-        public AuthenticationInformation(Uri authenticationMethod, DateTime authenticationInstant);

-        public string Address { get; set; }

-        public DateTime AuthenticationInstant { get; set; }

-        public Uri AuthenticationMethod { get; private set; }

-        public string DnsName { get; set; }

-        public Nullable<DateTime> NotOnOrAfter { get; set; }

-        public string Session { get; set; }

-    }
-    public static class ClaimProperties {
 {
-        public const string Namespace = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties";

-        public const string SamlAttributeFriendlyName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/friendlyname";

-        public const string SamlAttributeNameFormat = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/attributename";

-        public const string SamlNameIdentifierFormat = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/format";

-        public const string SamlNameIdentifierNameQualifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/namequalifier";

-        public const string SamlNameIdentifierSPNameQualifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/spnamequalifier";

-        public const string SamlNameIdentifierSPProvidedId = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/spprovidedid";

-    }
-    public class Saml2Action {
 {
-        public Saml2Action(string value, Uri @namespace);

-        public Uri Namespace { get; set; }

-        public string Value { get; set; }

-    }
-    public class Saml2Advice {
 {
-        public Saml2Advice();

-        public ICollection<Saml2Id> AssertionIdReferences { get; }

-        public ICollection<Saml2Assertion> Assertions { get; }

-        public ICollection<Uri> AssertionUriReferences { get; }

-    }
-    public class Saml2Assertion {
 {
-        public Saml2Assertion(Saml2NameIdentifier issuer);

-        public Saml2Advice Advice { get; set; }

-        public Saml2Conditions Conditions { get; set; }

-        public Saml2Id Id { get; set; }

-        public string InclusiveNamespacesPrefixList { get; set; }

-        public DateTime IssueInstant { get; set; }

-        public Saml2NameIdentifier Issuer { get; set; }

-        public Signature Signature { get; set; }

-        public SigningCredentials SigningCredentials { get; set; }

-        public ICollection<Saml2Statement> Statements { get; }

-        public Saml2Subject Subject { get; set; }

-        public string Version { get; }

-    }
-    public class Saml2Attribute {
 {
-        public Saml2Attribute(string name);

-        public Saml2Attribute(string name, IEnumerable<string> values);

-        public Saml2Attribute(string name, string value);

-        public string AttributeValueXsiType { get; set; }

-        public string FriendlyName { get; set; }

-        public string Name { get; set; }

-        public Uri NameFormat { get; set; }

-        public string OriginalIssuer { get; set; }

-        public ICollection<string> Values { get; }

-    }
-    public class Saml2AttributeStatement : Saml2Statement {
 {
-        public Saml2AttributeStatement();

-        public Saml2AttributeStatement(Saml2Attribute attribute);

-        public Saml2AttributeStatement(IEnumerable<Saml2Attribute> attributes);

-        public ICollection<Saml2Attribute> Attributes { get; }

-    }
-    public class Saml2AudienceRestriction {
 {
-        public Saml2AudienceRestriction(IEnumerable<string> audiences);

-        public Saml2AudienceRestriction(string audience);

-        public ICollection<string> Audiences { get; }

-    }
-    public class Saml2AuthenticationContext {
 {
-        public Saml2AuthenticationContext();

-        public Saml2AuthenticationContext(Uri classReference);

-        public Saml2AuthenticationContext(Uri classReference, Uri declarationReference);

-        public ICollection<Uri> AuthenticatingAuthorities { get; }

-        public Uri ClassReference { get; set; }

-        public Uri DeclarationReference { get; set; }

-    }
-    public class Saml2AuthenticationStatement : Saml2Statement {
 {
-        public Saml2AuthenticationStatement(Saml2AuthenticationContext authenticationContext);

-        public Saml2AuthenticationStatement(Saml2AuthenticationContext authenticationContext, DateTime authenticationInstant);

-        public Saml2AuthenticationContext AuthenticationContext { get; set; }

-        public DateTime AuthenticationInstant { get; set; }

-        public string SessionIndex { get; set; }

-        public Nullable<DateTime> SessionNotOnOrAfter { get; set; }

-        public Saml2SubjectLocality SubjectLocality { get; set; }

-    }
-    public class Saml2AuthorizationDecisionStatement : Saml2Statement {
 {
-        public Saml2AuthorizationDecisionStatement(Uri resource, string decision);

-        public Saml2AuthorizationDecisionStatement(Uri resource, string decision, IEnumerable<Saml2Action> actions);

-        public ICollection<Saml2Action> Actions { get; }

-        public string Decision { get; set; }

-        public Saml2Evidence Evidence { get; set; }

-        public Uri Resource { get; set; }

-    }
-    public class Saml2Conditions {
 {
-        public Saml2Conditions();

-        public Saml2Conditions(IEnumerable<Saml2AudienceRestriction> audienceRestrictions);

-        public ICollection<Saml2AudienceRestriction> AudienceRestrictions { get; }

-        public Nullable<DateTime> NotBefore { get; set; }

-        public Nullable<DateTime> NotOnOrAfter { get; set; }

-        public bool OneTimeUse { get; set; }

-        public Saml2ProxyRestriction ProxyRestriction { get; set; }

-    }
-    public static class Saml2Constants {
 {
-        public const string Namespace = "urn:oasis:names:tc:SAML:2.0:assertion";

-        public const string OasisWssSaml2TokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";

-        public const string Prefix = "saml";

-        public const string Saml2TokenProfile11 = "urn:oasis:names:tc:SAML:2.0:assertion";

-        public const string Version = "2.0";

-        public static string[] AcceptedDateTimeFormats;

-        public static class AccessDecision {
 {
-            public static string Deny { get; }

-            public static string Indeterminate { get; }

-            public static string Permit { get; }

-        }
-        public static class Attributes {
 {
-            public const string Address = "Address";

-            public const string AuthnInstant = "AuthnInstant";

-            public const string Count = "Count";

-            public const string Decision = "Decision";

-            public const string DNSName = "DNSName";

-            public const string Format = "Format";

-            public const string FriendlyName = "FriendlyName";

-            public const string ID = "ID";

-            public const string InResponseTo = "InResponseTo";

-            public const string IssueInstant = "IssueInstant";

-            public const string Method = "Method";

-            public const string Name = "Name";

-            public const string NameFormat = "NameFormat";

-            public const string NameQualifier = "NameQualifier";

-            public const string Namespace = "Namespace";

-            public const string NotBefore = "NotBefore";

-            public const string NotOnOrAfter = "NotOnOrAfter";

-            public const string OriginalIssuer = "OriginalIssuer";

-            public const string Recipient = "Recipient";

-            public const string Resource = "Resource";

-            public const string SessionIndex = "SessionIndex";

-            public const string SessionNotOnOrAfter = "SessionNotOnOrAfter";

-            public const string SPNameQualifier = "SPNameQualifier";

-            public const string SPProvidedID = "SPProvidedID";

-            public const string Type = "type";

-            public const string Version = "Version";

-        }
-        public static class ConfirmationMethods {
 {
-            public const string BearerString = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

-            public const string HolderOfKeyString = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";

-            public const string SenderVouchesString = "urn:oasis:names:tc:SAML:2.0:cm:sender-vouches";

-            public static readonly Uri Bearer;

-            public static readonly Uri HolderOfKey;

-            public static readonly Uri SenderVouches;

-        }
-        public static class Elements {
 {
-            public const string Action = "Action";

-            public const string Advice = "Advice";

-            public const string Assertion = "Assertion";

-            public const string AssertionIDRef = "AssertionIDRef";

-            public const string AssertionURIRef = "AssertionURIRef";

-            public const string Attribute = "Attribute";

-            public const string AttributeStatement = "AttributeStatement";

-            public const string AttributeValue = "AttributeValue";

-            public const string Audience = "Audience";

-            public const string AudienceRestriction = "AudienceRestriction";

-            public const string AuthenticatingAuthority = "AuthenticatingAuthority";

-            public const string AuthnContext = "AuthnContext";

-            public const string AuthnContextClassRef = "AuthnContextClassRef";

-            public const string AuthnContextDecl = "AuthnContextDecl";

-            public const string AuthnContextDeclRef = "AuthnContextDeclRef";

-            public const string AuthnStatement = "AuthnStatement";

-            public const string AuthzDecisionStatement = "AuthzDecisionStatement";

-            public const string BaseID = "BaseID";

-            public const string Condition = "Condition";

-            public const string Conditions = "Conditions";

-            public const string EncryptedAssertion = "EncryptedAssertion";

-            public const string EncryptedAttribute = "EncryptedAttribute";

-            public const string EncryptedID = "EncryptedID";

-            public const string Evidence = "Evidence";

-            public const string Issuer = "Issuer";

-            public const string NameID = "NameID";

-            public const string OneTimeUse = "OneTimeUse";

-            public const string ProxyRestricton = "ProxyRestriction";

-            public const string Statement = "Statement";

-            public const string Subject = "Subject";

-            public const string SubjectConfirmation = "SubjectConfirmation";

-            public const string SubjectConfirmationData = "SubjectConfirmationData";

-            public const string SubjectLocality = "SubjectLocality";

-        }
-        public static class NameIdentifierFormats {
 {
-            public const string EmailAddressString = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";

-            public const string EncryptedString = "urn:oasis:names:tc:SAML:2.0:nameid-format:encrypted";

-            public const string EntityString = "urn:oasis:names:tc:SAML:2.0:nameid-format:entity";

-            public const string KerberosString = "urn:oasis:names:tc:SAML:2.0:nameid-format:kerberos";

-            public const string PersistentString = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent";

-            public const string TransientString = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";

-            public const string UnspecifiedString = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";

-            public const string WindowsDomainQualifiedNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName";

-            public const string X509SubjectNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName";

-            public static readonly Uri EmailAddress;

-            public static readonly Uri Encrypted;

-            public static readonly Uri Entity;

-            public static readonly Uri Kerberos;

-            public static readonly Uri Persistent;

-            public static readonly Uri Transient;

-            public static readonly Uri Unspecified;

-            public static readonly Uri WindowsDomainQualifiedName;

-            public static readonly Uri X509SubjectName;

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

-            public const string AuthzDecisionStatementType = "AuthzDecisionStatementType";

-            public const string BaseIDAbstractType = "BaseIDAbstractType";

-            public const string ConditionAbstractType = "ConditionAbstractType";

-            public const string ConditionsType = "ConditionsType";

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
-    public class Saml2Evidence {
 {
-        public Saml2Evidence();

-        public Saml2Evidence(Saml2Assertion assertion);

-        public Saml2Evidence(Saml2Id idReference);

-        public Saml2Evidence(Uri uriReference);

-        public ICollection<Saml2Id> AssertionIdReferences { get; }

-        public ICollection<Saml2Assertion> Assertions { get; }

-        public ICollection<Uri> AssertionUriReferences { get; }

-    }
-    public class Saml2Id {
 {
-        public Saml2Id();

-        public Saml2Id(string value);

-        public string Value { get; }

-    }
-    public class Saml2NameIdentifier {
 {
-        public Saml2NameIdentifier(string name);

-        public Saml2NameIdentifier(string name, Uri format);

-        public EncryptingCredentials EncryptingCredentials { get; set; }

-        public Uri Format { get; set; }

-        public string NameQualifier { get; set; }

-        public string SPNameQualifier { get; set; }

-        public string SPProvidedId { get; set; }

-        public string Value { get; set; }

-    }
-    public class Saml2ProxyRestriction {
 {
-        public Saml2ProxyRestriction();

-        public ICollection<Uri> Audiences { get; }

-        public Nullable<int> Count { get; set; }

-    }
-    public class Saml2SecurityToken : SecurityToken {
 {
-        public Saml2SecurityToken(Saml2Assertion assertion);

-        public Saml2Assertion Assertion { get; }

-        public override string Id { get; }

-        public override string Issuer { get; }

-        public override SecurityKey SecurityKey { get; }

-        public override SecurityKey SigningKey { get; set; }

-        public override DateTime ValidFrom { get; }

-        public override DateTime ValidTo { get; }

-    }
-    public class Saml2SecurityTokenException : SecurityTokenException {
 {
-        public Saml2SecurityTokenException();

-        public Saml2SecurityTokenException(string message);

-        public Saml2SecurityTokenException(string message, Exception innerException);

-    }
-    public class Saml2SecurityTokenHandler : SecurityTokenHandler {
 {
-        public Saml2SecurityTokenHandler();

-        public override bool CanValidateToken { get; }

-        public override bool CanWriteToken { get; }

-        public Saml2Serializer Serializer { get; set; }

-        public override Type TokenType { get; }

-        public override bool CanReadToken(string token);

-        public bool CanReadToken(XmlReader reader);

-        protected virtual ICollection<Saml2Attribute> ConsolidateAttributes(ICollection<Saml2Attribute> attributes);

-        protected string CreateActorString(ClaimsIdentity actor);

-        protected virtual Saml2Advice CreateAdvice(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual Saml2Attribute CreateAttribute(Claim claim);

-        protected virtual Saml2AttributeStatement CreateAttributeStatement(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual Saml2AuthenticationStatement CreateAuthenticationStatement(AuthenticationInformation authenticationInformation);

-        public virtual Saml2AuthorizationDecisionStatement CreateAuthorizationDecisionStatement(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual ClaimsIdentity CreateClaimsIdentity(Saml2SecurityToken samlToken, string issuer, TokenValidationParameters validationParameters);

-        protected virtual Saml2Conditions CreateConditions(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual Saml2NameIdentifier CreateIssuerNameIdentifier(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual IEnumerable<Saml2Statement> CreateStatements(SecurityTokenDescriptor tokenDescriptor);

-        protected virtual IEnumerable<Saml2Statement> CreateStatements(SecurityTokenDescriptor tokenDescriptor, AuthenticationInformation authenticationInformation);

-        protected virtual Saml2Subject CreateSubject(SecurityTokenDescriptor tokenDescriptor);

-        public override SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor);

-        public virtual SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor, AuthenticationInformation authenticationInformation);

-        protected virtual void ProcessAttributeStatement(Saml2AttributeStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessAuthenticationStatement(Saml2AuthenticationStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessAuthorizationDecisionStatement(Saml2AuthorizationDecisionStatement statement, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessStatements(ICollection<Saml2Statement> statements, ClaimsIdentity identity, string issuer);

-        protected virtual void ProcessSubject(Saml2Subject subject, ClaimsIdentity identity, string issuer);

-        public virtual Saml2SecurityToken ReadSaml2Token(string token);

-        public override SecurityToken ReadToken(string token);

-        public override SecurityToken ReadToken(XmlReader reader, TokenValidationParameters validationParameters);

-        protected virtual SecurityKey ResolveIssuerSigningKey(string token, Saml2SecurityToken samlToken, TokenValidationParameters validationParameters);

-        protected virtual void SetClaimsIdentityActorFromAttribute(Saml2Attribute attribute, ClaimsIdentity identity, string issuer);

-        protected virtual void ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateConditions(Saml2SecurityToken samlToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateConfirmationData(Saml2SecurityToken samlToken, TokenValidationParameters validationParameters, Saml2SubjectConfirmationData confirmationData);

-        protected virtual string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual void ValidateIssuerSecurityKey(SecurityKey key, Saml2SecurityToken securityToken, TokenValidationParameters validationParameters);

-        protected virtual Saml2SecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters);

-        protected virtual void ValidateSubject(Saml2SecurityToken samlToken, TokenValidationParameters validationParameters);

-        public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken);

-        protected virtual void ValidateTokenReplay(Nullable<DateTime> expirationTime, string securityToken, TokenValidationParameters validationParameters);

-        public override string WriteToken(SecurityToken securityToken);

-        public override void WriteToken(XmlWriter writer, SecurityToken securityToken);

-    }
-    public class Saml2SecurityTokenReadException : Saml2SecurityTokenException {
 {
-        public Saml2SecurityTokenReadException();

-        public Saml2SecurityTokenReadException(string message);

-        public Saml2SecurityTokenReadException(string message, Exception innerException);

-    }
-    public class Saml2SecurityTokenWriteException : Saml2SecurityTokenException {
 {
-        public Saml2SecurityTokenWriteException();

-        public Saml2SecurityTokenWriteException(string message);

-        public Saml2SecurityTokenWriteException(string message, Exception innerException);

-    }
-    public class Saml2Serializer {
 {
-        public Saml2Serializer();

-        public DSigSerializer DSigSerializer { get; set; }

-        public string Prefix { get; set; }

-        protected virtual Saml2Action ReadAction(XmlDictionaryReader reader);

-        protected virtual Saml2Advice ReadAdvice(XmlDictionaryReader reader);

-        public virtual Saml2Assertion ReadAssertion(XmlReader reader);

-        public virtual Saml2Attribute ReadAttribute(XmlDictionaryReader reader);

-        protected virtual Saml2AttributeStatement ReadAttributeStatement(XmlDictionaryReader reader);

-        protected virtual string ReadAttributeValue(XmlDictionaryReader reader, Saml2Attribute attribute);

-        protected virtual Saml2AudienceRestriction ReadAudienceRestriction(XmlDictionaryReader reader);

-        protected virtual Saml2AuthenticationContext ReadAuthenticationContext(XmlDictionaryReader reader);

-        protected virtual Saml2AuthenticationStatement ReadAuthenticationStatement(XmlDictionaryReader reader);

-        protected virtual Saml2AuthorizationDecisionStatement ReadAuthorizationDecisionStatement(XmlDictionaryReader reader);

-        protected virtual Saml2Conditions ReadConditions(XmlDictionaryReader reader);

-        protected virtual Saml2NameIdentifier ReadEncryptedId(XmlDictionaryReader reader);

-        protected virtual Saml2Evidence ReadEvidence(XmlDictionaryReader reader);

-        protected virtual Saml2NameIdentifier ReadIssuer(XmlDictionaryReader reader);

-        protected virtual Saml2NameIdentifier ReadNameId(XmlDictionaryReader reader);

-        protected virtual Saml2NameIdentifier ReadNameIdentifier(XmlDictionaryReader reader, string parentElement);

-        protected virtual Saml2ProxyRestriction ReadProxyRestriction(XmlDictionaryReader reader);

-        protected virtual Saml2Statement ReadStatement(XmlDictionaryReader reader);

-        protected virtual Saml2Subject ReadSubject(XmlDictionaryReader reader);

-        protected virtual Saml2SubjectConfirmation ReadSubjectConfirmation(XmlDictionaryReader reader);

-        protected virtual Saml2SubjectConfirmationData ReadSubjectConfirmationData(XmlDictionaryReader reader);

-        protected virtual Saml2SubjectLocality ReadSubjectLocality(XmlDictionaryReader reader);

-        protected virtual void WriteAction(XmlWriter writer, Saml2Action action);

-        protected virtual void WriteAdvice(XmlWriter writer, Saml2Advice advice);

-        public virtual void WriteAssertion(XmlWriter writer, Saml2Assertion assertion);

-        public virtual void WriteAttribute(XmlWriter writer, Saml2Attribute attribute);

-        protected virtual void WriteAttributeStatement(XmlWriter writer, Saml2AttributeStatement statement);

-        protected virtual void WriteAudienceRestriction(XmlWriter writer, Saml2AudienceRestriction audienceRestriction);

-        protected virtual void WriteAuthenticationContext(XmlWriter writer, Saml2AuthenticationContext authenticationContext);

-        protected virtual void WriteAuthenticationStatement(XmlWriter writer, Saml2AuthenticationStatement statement);

-        protected virtual void WriteAuthorizationDecisionStatement(XmlWriter writer, Saml2AuthorizationDecisionStatement statement);

-        protected virtual void WriteConditions(XmlWriter writer, Saml2Conditions conditions);

-        protected virtual void WriteEvidence(XmlWriter writer, Saml2Evidence evidence);

-        protected virtual void WriteIssuer(XmlWriter writer, Saml2NameIdentifier nameIdentifier);

-        protected virtual void WriteNameId(XmlWriter writer, Saml2NameIdentifier nameIdentifier);

-        protected virtual void WriteNameIdType(XmlWriter writer, Saml2NameIdentifier nameIdentifier);

-        protected virtual void WriteProxyRestriction(XmlWriter writer, Saml2ProxyRestriction proxyRestriction);

-        protected virtual void WriteStatement(XmlWriter writer, Saml2Statement statement);

-        protected virtual void WriteSubject(XmlWriter writer, Saml2Subject subject);

-        protected virtual void WriteSubjectConfirmation(XmlWriter writer, Saml2SubjectConfirmation subjectConfirmation);

-        protected virtual void WriteSubjectConfirmationData(XmlWriter writer, Saml2SubjectConfirmationData subjectConfirmationData);

-        protected virtual void WriteSubjectLocality(XmlWriter writer, Saml2SubjectLocality subjectLocality);

-    }
-    public abstract class Saml2Statement {
 {
-        protected Saml2Statement();

-    }
-    public class Saml2Subject {
 {
-        public Saml2Subject(Saml2NameIdentifier nameId);

-        public Saml2Subject(Saml2SubjectConfirmation subjectConfirmation);

-        public Saml2NameIdentifier NameId { get; set; }

-        public ICollection<Saml2SubjectConfirmation> SubjectConfirmations { get; }

-    }
-    public class Saml2SubjectConfirmation {
 {
-        public Saml2SubjectConfirmation(Uri method);

-        public Saml2SubjectConfirmation(Uri method, Saml2SubjectConfirmationData subjectConfirmationData);

-        public Uri Method { get; set; }

-        public Saml2NameIdentifier NameIdentifier { get; set; }

-        public Saml2SubjectConfirmationData SubjectConfirmationData { get; set; }

-    }
-    public class Saml2SubjectConfirmationData {
 {
-        public Saml2SubjectConfirmationData();

-        public string Address { get; set; }

-        public Saml2Id InResponseTo { get; set; }

-        public ICollection<KeyInfo> KeyInfos { get; }

-        public Nullable<DateTime> NotBefore { get; set; }

-        public Nullable<DateTime> NotOnOrAfter { get; set; }

-        public Uri Recipient { get; set; }

-    }
-    public class Saml2SubjectLocality {
 {
-        public Saml2SubjectLocality(string address, string dnsName);

-        public string Address { get; set; }

-        public string DnsName { get; set; }

-    }
-}
```

