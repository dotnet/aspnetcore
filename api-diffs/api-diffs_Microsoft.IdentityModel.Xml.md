# Microsoft.IdentityModel.Xml

``` diff
-namespace Microsoft.IdentityModel.Xml {
 {
-    public abstract class CanonicalizingTransfrom {
 {
-        protected CanonicalizingTransfrom();

-        public abstract string Algorithm { get; }

-        public bool IncludeComments { get; set; }

-        public string InclusiveNamespacesPrefixList { get; set; }

-        public abstract byte[] ProcessAndDigest(XmlTokenStream tokenStream, HashAlgorithm hashAlg);

-    }
-    public class DelegatingXmlDictionaryReader : XmlDictionaryReader, IXmlLineInfo {
 {
-        protected DelegatingXmlDictionaryReader();

-        public override int AttributeCount { get; }

-        public override string BaseURI { get; }

-        public override bool CanReadBinaryContent { get; }

-        public override bool CanReadValueChunk { get; }

-        public override int Depth { get; }

-        public override bool EOF { get; }

-        public override bool HasValue { get; }

-        protected XmlDictionaryReader InnerReader { get; set; }

-        public override bool IsDefault { get; }

-        public override bool IsEmptyElement { get; }

-        public int LineNumber { get; }

-        public int LinePosition { get; }

-        public override string LocalName { get; }

-        public override string Name { get; }

-        public override string NamespaceURI { get; }

-        public override XmlNameTable NameTable { get; }

-        public override XmlNodeType NodeType { get; }

-        public override string Prefix { get; }

-        public override ReadState ReadState { get; }

-        public override string this[int i] { get; }

-        public override string this[string name, string @namespace] { get; }

-        public override string this[string name] { get; }

-        protected XmlDictionaryReader UseInnerReader { get; }

-        public override string Value { get; }

-        public override Type ValueType { get; }

-        public override string XmlLang { get; }

-        public override XmlSpace XmlSpace { get; }

-        public override string GetAttribute(int i);

-        public override string GetAttribute(string name);

-        public override string GetAttribute(string name, string @namespace);

-        public bool HasLineInfo();

-        public override string LookupNamespace(string prefix);

-        public override void MoveToAttribute(int index);

-        public override bool MoveToAttribute(string name);

-        public override bool MoveToAttribute(string name, string @namespace);

-        public override bool MoveToElement();

-        public override bool MoveToFirstAttribute();

-        public override bool MoveToNextAttribute();

-        public override bool Read();

-        public override bool ReadAttributeValue();

-        public override int ReadContentAsBase64(byte[] buffer, int index, int count);

-        public override int ReadContentAsBinHex(byte[] buffer, int index, int count);

-        public override int ReadValueChunk(char[] buffer, int index, int count);

-        public override void ResolveEntity();

-    }
-    public class DelegatingXmlDictionaryWriter : XmlDictionaryWriter {
 {
-        protected DelegatingXmlDictionaryWriter();

-        protected XmlDictionaryWriter InnerWriter { get; set; }

-        protected XmlDictionaryWriter TracingWriter { get; set; }

-        protected XmlDictionaryWriter UseInnerWriter { get; }

-        public override WriteState WriteState { get; }

-        public override void Flush();

-        public override string LookupPrefix(string @namespace);

-        public override void WriteBase64(byte[] buffer, int index, int count);

-        public override void WriteCData(string text);

-        public override void WriteCharEntity(char ch);

-        public override void WriteChars(char[] buffer, int index, int count);

-        public override void WriteComment(string text);

-        public override void WriteDocType(string name, string pubid, string sysid, string subset);

-        public override void WriteEndAttribute();

-        public override void WriteEndDocument();

-        public override void WriteEndElement();

-        public override void WriteEntityRef(string name);

-        public override void WriteFullEndElement();

-        public override void WriteProcessingInstruction(string name, string text);

-        public override void WriteRaw(char[] buffer, int index, int count);

-        public override void WriteRaw(string data);

-        public override void WriteStartAttribute(string prefix, string localName, string @namespace);

-        public override void WriteStartDocument();

-        public override void WriteStartDocument(bool standalone);

-        public override void WriteStartElement(string prefix, string localName, string @namespace);

-        public override void WriteString(string text);

-        public override void WriteSurrogateCharEntity(char lowChar, char highChar);

-        public override void WriteWhitespace(string ws);

-        public override void WriteXmlAttribute(string localName, string value);

-        public override void WriteXmlnsAttribute(string prefix, string @namespace);

-    }
-    public class DSigElement {
 {
-        protected DSigElement();

-        public string Id { get; set; }

-        public string Prefix { get; set; }

-    }
-    public class DSigSerializer {
 {
-        public DSigSerializer();

-        public static DSigSerializer Default { get; set; }

-        public string Prefix { get; set; }

-        public TransformFactory TransformFactory { get; set; }

-        public virtual string ReadCanonicalizationMethod(XmlReader reader);

-        public virtual KeyInfo ReadKeyInfo(XmlReader reader);

-        public virtual Reference ReadReference(XmlReader reader);

-        public virtual Signature ReadSignature(XmlReader reader);

-        public virtual string ReadSignatureMethod(XmlReader reader);

-        public virtual SignedInfo ReadSignedInfo(XmlReader reader);

-        public virtual void ReadTransforms(XmlReader reader, Reference reference);

-        public virtual void WriteKeyInfo(XmlWriter writer, KeyInfo keyInfo);

-        public virtual void WriteReference(XmlWriter writer, Reference reference);

-        public virtual void WriteSignature(XmlWriter writer, Signature signature);

-        public virtual void WriteSignedInfo(XmlWriter writer, SignedInfo signedInfo);

-    }
-    public class EnvelopedSignatureReader : DelegatingXmlDictionaryReader {
 {
-        public EnvelopedSignatureReader(XmlReader reader);

-        public DSigSerializer Serializer { get; set; }

-        public Signature Signature { get; protected set; }

-        protected virtual void OnEndOfRootElement();

-        public override bool Read();

-    }
-    public class EnvelopedSignatureTransform : Transform {
 {
-        public EnvelopedSignatureTransform();

-        public override string Algorithm { get; }

-        public override XmlTokenStream Process(XmlTokenStream tokenStream);

-    }
-    public class EnvelopedSignatureWriter : DelegatingXmlDictionaryWriter {
 {
-        public EnvelopedSignatureWriter(XmlWriter writer, SigningCredentials signingCredentials, string referenceId);

-        public EnvelopedSignatureWriter(XmlWriter writer, SigningCredentials signingCredentials, string referenceId, string inclusivePrefixList);

-        public DSigSerializer DSigSerializer { get; set; }

-        protected override void Dispose(bool disposing);

-        public override void WriteEndElement();

-        public override void WriteFullEndElement();

-        public void WriteSignature();

-        public override void WriteStartElement(string prefix, string localName, string @namespace);

-    }
-    public class ExclusiveCanonicalizationTransform : CanonicalizingTransfrom {
 {
-        public ExclusiveCanonicalizationTransform();

-        public ExclusiveCanonicalizationTransform(bool includeComments);

-        public override string Algorithm { get; }

-        public override byte[] ProcessAndDigest(XmlTokenStream tokenStream, HashAlgorithm hash);

-    }
-    public class IssuerSerial {
 {
-        public IssuerSerial(string issuerName, string serialNumber);

-        public string IssuerName { get; }

-        public string SerialNumber { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class KeyInfo : DSigElement {
 {
-        public KeyInfo();

-        public KeyInfo(SecurityKey key);

-        public KeyInfo(X509Certificate2 certificate);

-        public string KeyName { get; set; }

-        public string RetrievalMethodUri { get; set; }

-        public RSAKeyValue RSAKeyValue { get; set; }

-        public ICollection<X509Data> X509Data { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class Reference : DSigElement {
 {
-        public Reference();

-        public Reference(Transform transform, CanonicalizingTransfrom canonicalizingTransfrom);

-        public CanonicalizingTransfrom CanonicalizingTransfrom { get; set; }

-        public string DigestMethod { get; set; }

-        public string DigestValue { get; set; }

-        public XmlTokenStream TokenStream { get; set; }

-        public IList<Transform> Transforms { get; }

-        public string Type { get; set; }

-        public string Uri { get; set; }

-        protected byte[] ComputeDigest(CryptoProviderFactory cryptoProviderFactory);

-        public void Verify(CryptoProviderFactory cryptoProviderFactory);

-    }
-    public class RSAKeyValue {
 {
-        public RSAKeyValue(string modulus, string exponent);

-        public string Exponent { get; }

-        public string Modulus { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class Signature : DSigElement {
 {
-        public Signature();

-        public Signature(SignedInfo signedInfo);

-        public KeyInfo KeyInfo { get; set; }

-        public string SignatureValue { get; set; }

-        public SignedInfo SignedInfo { get; set; }

-        public void Verify(SecurityKey key, CryptoProviderFactory cryptoProviderFactory);

-    }
-    public class SignedInfo : DSigElement {
 {
-        public SignedInfo();

-        public SignedInfo(Reference reference);

-        public string CanonicalizationMethod { get; set; }

-        public IList<Reference> References { get; }

-        public string SignatureMethod { get; set; }

-        public void GetCanonicalBytes(Stream stream);

-        public void Verify(CryptoProviderFactory cryptoProviderFactory);

-    }
-    public abstract class Transform {
 {
-        protected Transform();

-        public abstract string Algorithm { get; }

-        public abstract XmlTokenStream Process(XmlTokenStream tokenStream);

-    }
-    public class TransformFactory {
 {
-        public TransformFactory();

-        public static TransformFactory Default { get; }

-        public virtual CanonicalizingTransfrom GetCanonicalizingTransform(string transform);

-        public virtual Transform GetTransform(string transform);

-        public virtual bool IsSupportedCanonicalizingTransfrom(string transform);

-        public virtual bool IsSupportedTransform(string transform);

-    }
-    public static class WsAddressing {
 {
-        public const string Namespace = "http://www.w3.org/2005/08/addressing";

-        public const string PreferredPrefix = "wsa";

-        public static class Elements {
 {
-            public const string Address = "Address";

-            public const string EndpointReference = "EndpointReference";

-        }
-    }
-    public static class WsPolicy {
 {
-        public const string Namespace = "http://schemas.xmlsoap.org/ws/2004/09/policy";

-        public const string PreferredPrefix = "wsp";

-        public static class Elements {
 {
-            public const string AppliesTo = "AppliesTo";

-        }
-    }
-    public static class WsTrustConstants {
 {
-        public static class Elements {
 {
-            public const string KeyType = "KeyType";

-            public const string Lifetime = "Lifetime";

-            public const string RequestedAttachedReference = "RequestedAttachedReference";

-            public const string RequestedSecurityToken = "RequestedSecurityToken";

-            public const string RequestedUnattachedReference = "RequestedUnattachedReference";

-            public const string RequestSecurityTokenResponse = "RequestSecurityTokenResponse";

-            public const string RequestSecurityTokenResponseCollection = "RequestSecurityTokenResponseCollection";

-            public const string RequestType = "RequestType";

-            public const string SecurityTokenReference = "SecurityTokenReference";

-            public const string TokenType = "TokenType";

-        }
-        public static class Namespaces {
 {
-            public const string WsTrust1_3 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";

-            public const string WsTrust1_4 = "http://docs.oasis-open.org/ws-sx/ws-trust/200802";

-            public const string WsTrust2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust";

-        }
-    }
-    public static class WsTrustConstants_1_3 {
 {
-        public const string Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";

-        public const string PreferredPrefix = "t";

-        public static class Actions {
 {
-            public const string Issue = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";

-        }
-    }
-    public static class WsTrustConstants_1_4 {
 {
-        public const string Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200802";

-        public const string PreferredPrefix = "t";

-        public static class Actions {
 {
-            public const string Issue = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";

-        }
-    }
-    public static class WsTrustConstants_2005 {
 {
-        public const string Namespace = "http://schemas.xmlsoap.org/ws/2005/02/trust";

-        public const string PreferredPrefix = "trust";

-        public static class Actions {
 {
-            public const string Issue = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";

-        }
-    }
-    public static class WsUtility {
 {
-        public const string Namespace = "http://www.w3.org/2005/08/addressing";

-        public const string PreferredPrefix = "wsu";

-        public static class Elements {
 {
-            public const string Created = "Created";

-            public const string Expires = "Expires";

-        }
-    }
-    public class X509Data {
 {
-        public X509Data();

-        public X509Data(IEnumerable<X509Certificate2> certificates);

-        public X509Data(X509Certificate2 certificate);

-        public ICollection<string> Certificates { get; }

-        public string CRL { get; set; }

-        public IssuerSerial IssuerSerial { get; set; }

-        public string SKI { get; set; }

-        public string SubjectName { get; set; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class XmlException : Exception {
 {
-        public XmlException();

-        public XmlException(string message);

-        public XmlException(string message, Exception innerException);

-    }
-    public class XmlReadException : XmlException {
 {
-        public XmlReadException();

-        public XmlReadException(string message);

-        public XmlReadException(string message, Exception innerException);

-    }
-    public static class XmlSignatureConstants {
 {
-        public const string ExclusiveC14nInclusiveNamespaces = "InclusiveNamespaces";

-        public const string ExclusiveC14nNamespace = "http://www.w3.org/2001/10/xml-exc-c14n#";

-        public const string ExclusiveC14nPrefix = "ec";

-        public const string Namespace = "http://www.w3.org/2000/09/xmldsig#";

-        public const string PreferredPrefix = "ds";

-        public const string SecurityJan2004Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

-        public const string SecurityJan2004Prefix = "o";

-        public const string TransformationParameters = "TransformationParameters";

-        public const string XmlNamepspacePrefix = "xmlns";

-        public const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

-        public const string XmlNamespaceNamespace = "http://www.w3.org/2000/xmlns/";

-        public const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema-instance";

-        public static class Attributes {
 {
-            public const string Algorithm = "Algorithm";

-            public const string AnyUri = "anyURI";

-            public const string Id = "Id";

-            public const string NcName = "NCName";

-            public const string Nil = "nil";

-            public const string PrefixList = "PrefixList";

-            public const string Type = "Type";

-            public const string URI = "URI";

-        }
-        public static class Elements {
 {
-            public const string CanonicalizationMethod = "CanonicalizationMethod";

-            public const string DigestMethod = "DigestMethod";

-            public const string DigestValue = "DigestValue";

-            public const string Exponent = "Exponent";

-            public const string InclusiveNamespaces = "InclusiveNamespaces";

-            public const string KeyInfo = "KeyInfo";

-            public const string KeyName = "KeyName";

-            public const string KeyValue = "KeyValue";

-            public const string Modulus = "Modulus";

-            public const string Object = "Object";

-            public const string Reference = "Reference";

-            public const string RetrievalMethod = "RetrievalMethod";

-            public const string RSAKeyValue = "RSAKeyValue";

-            public const string Signature = "Signature";

-            public const string SignatureMethod = "SignatureMethod";

-            public const string SignatureValue = "SignatureValue";

-            public const string SignedInfo = "SignedInfo";

-            public const string Transform = "Transform";

-            public const string TransformationParameters = "TransformationParameters";

-            public const string Transforms = "Transforms";

-            public const string X509Certificate = "X509Certificate";

-            public const string X509CRL = "X509CRL";

-            public const string X509Data = "X509Data";

-            public const string X509IssuerName = "X509IssuerName";

-            public const string X509IssuerSerial = "X509IssuerSerial";

-            public const string X509SerialNumber = "X509SerialNumber";

-            public const string X509SKI = "X509SKI";

-            public const string X509SubjectName = "X509SubjectName";

-        }
-    }
-    public class XmlTokenStream {
 {
-        public XmlTokenStream();

-        public void Add(XmlNodeType type, string value);

-        public void AddAttribute(string prefix, string localName, string @namespace, string value);

-        public void AddElement(string prefix, string localName, string @namespace, bool isEmptyElement);

-        public void SetElementExclusion(string element, string @namespace);

-        public void WriteTo(XmlWriter writer);

-    }
-    public class XmlTokenStreamReader : DelegatingXmlDictionaryReader {
 {
-        public XmlTokenStreamReader(XmlDictionaryReader reader);

-        public XmlTokenStream TokenStream { get; }

-        public override bool Read();

-    }
-    public static class XmlUtil {
 {
-        public static void CheckReaderOnEntry(XmlReader reader, string element);

-        public static void CheckReaderOnEntry(XmlReader reader, string element, string @namespace);

-        public static bool EqualsQName(XmlQualifiedName qualifiedName, string name, string @namespace);

-        public static XmlQualifiedName GetXsiTypeAsQualifiedName(XmlReader reader);

-        public static bool IsNil(XmlReader reader);

-        public static bool IsStartElement(XmlReader reader, string element, ICollection<string> namespaceList);

-        public static Exception LogReadException(string format, Exception inner, params object[] args);

-        public static Exception LogReadException(string format, params object[] args);

-        public static Exception LogValidationException(string format, Exception inner, params object[] args);

-        public static Exception LogValidationException(string format, params object[] args);

-        public static Exception LogWriteException(string format, Exception inner, params object[] args);

-        public static Exception LogWriteException(string format, params object[] args);

-        public static string NormalizeEmptyString(string @string);

-        public static XmlQualifiedName ResolveQName(XmlReader reader, string qualifiedString);

-        public static void ValidateXsiType(XmlReader reader, string expectedTypeName, string expectedTypeNamespace);

-        public static void ValidateXsiType(XmlReader reader, string expectedTypeName, string expectedTypeNamespace, bool requireDeclaration);

-    }
-    public class XmlValidationException : XmlException {
 {
-        public XmlValidationException();

-        public XmlValidationException(string message);

-        public XmlValidationException(string message, Exception innerException);

-    }
-    public class XmlWriteException : XmlException {
 {
-        public XmlWriteException();

-        public XmlWriteException(string message);

-        public XmlWriteException(string message, Exception innerException);

-    }
-}
```

