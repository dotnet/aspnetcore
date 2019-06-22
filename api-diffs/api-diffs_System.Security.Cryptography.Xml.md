# System.Security.Cryptography.Xml

``` diff
 namespace System.Security.Cryptography.Xml {
     public sealed class CipherData {
         public CipherData();
         public CipherData(byte[] cipherValue);
         public CipherData(CipherReference cipherReference);
         public CipherReference CipherReference { get; set; }
         public byte[] CipherValue { get; set; }
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public sealed class CipherReference : EncryptedReference {
         public CipherReference();
         public CipherReference(string uri);
         public CipherReference(string uri, TransformChain transformChain);
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
-    public class CryptoSignedXmlRecursionException : XmlException {
 {
-        public CryptoSignedXmlRecursionException();

-        protected CryptoSignedXmlRecursionException(SerializationInfo info, StreamingContext context);

-        public CryptoSignedXmlRecursionException(string message);

-        public CryptoSignedXmlRecursionException(string message, Exception inner);

-    }
     public class DataObject {
         public DataObject();
         public DataObject(string id, string mimeType, string encoding, XmlElement data);
         public XmlNodeList Data { get; set; }
         public string Encoding { get; set; }
         public string Id { get; set; }
         public string MimeType { get; set; }
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public sealed class DataReference : EncryptedReference {
         public DataReference();
         public DataReference(string uri);
         public DataReference(string uri, TransformChain transformChain);
     }
     public class DSAKeyValue : KeyInfoClause {
         public DSAKeyValue();
         public DSAKeyValue(DSA key);
         public DSA Key { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public sealed class EncryptedData : EncryptedType {
         public EncryptedData();
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public sealed class EncryptedKey : EncryptedType {
         public EncryptedKey();
         public string CarriedKeyName { get; set; }
         public string Recipient { get; set; }
         public ReferenceList ReferenceList { get; }
         public void AddReference(DataReference dataReference);
         public void AddReference(KeyReference keyReference);
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public abstract class EncryptedReference {
         protected EncryptedReference();
         protected EncryptedReference(string uri);
         protected EncryptedReference(string uri, TransformChain transformChain);
         protected internal bool CacheValid { get; }
         protected string ReferenceType { get; set; }
         public TransformChain TransformChain { get; set; }
         public string Uri { get; set; }
         public void AddTransform(Transform transform);
         public virtual XmlElement GetXml();
         public virtual void LoadXml(XmlElement value);
     }
     public abstract class EncryptedType {
         protected EncryptedType();
         public virtual CipherData CipherData { get; set; }
         public virtual string Encoding { get; set; }
         public virtual EncryptionMethod EncryptionMethod { get; set; }
         public virtual EncryptionPropertyCollection EncryptionProperties { get; }
         public virtual string Id { get; set; }
         public KeyInfo KeyInfo { get; set; }
         public virtual string MimeType { get; set; }
         public virtual string Type { get; set; }
         public void AddProperty(EncryptionProperty ep);
         public abstract XmlElement GetXml();
         public abstract void LoadXml(XmlElement value);
     }
     public class EncryptedXml {
         public const string XmlEncAES128KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes128";
         public const string XmlEncAES128Url = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";
         public const string XmlEncAES192KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes192";
         public const string XmlEncAES192Url = "http://www.w3.org/2001/04/xmlenc#aes192-cbc";
         public const string XmlEncAES256KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes256";
         public const string XmlEncAES256Url = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
         public const string XmlEncDESUrl = "http://www.w3.org/2001/04/xmlenc#des-cbc";
         public const string XmlEncElementContentUrl = "http://www.w3.org/2001/04/xmlenc#Content";
         public const string XmlEncElementUrl = "http://www.w3.org/2001/04/xmlenc#Element";
         public const string XmlEncEncryptedKeyUrl = "http://www.w3.org/2001/04/xmlenc#EncryptedKey";
         public const string XmlEncNamespaceUrl = "http://www.w3.org/2001/04/xmlenc#";
         public const string XmlEncRSA15Url = "http://www.w3.org/2001/04/xmlenc#rsa-1_5";
         public const string XmlEncRSAOAEPUrl = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";
         public const string XmlEncSHA256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
         public const string XmlEncSHA512Url = "http://www.w3.org/2001/04/xmlenc#sha512";
         public const string XmlEncTripleDESKeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-tripledes";
         public const string XmlEncTripleDESUrl = "http://www.w3.org/2001/04/xmlenc#tripledes-cbc";
         public EncryptedXml();
         public EncryptedXml(XmlDocument document);
         public EncryptedXml(XmlDocument document, Evidence evidence);
         public Evidence DocumentEvidence { get; set; }
         public Encoding Encoding { get; set; }
         public CipherMode Mode { get; set; }
         public PaddingMode Padding { get; set; }
         public string Recipient { get; set; }
         public XmlResolver Resolver { get; set; }
         public int XmlDSigSearchDepth { get; set; }
         public void AddKeyNameMapping(string keyName, object keyObject);
         public void ClearKeyNameMappings();
         public byte[] DecryptData(EncryptedData encryptedData, SymmetricAlgorithm symmetricAlgorithm);
         public void DecryptDocument();
         public virtual byte[] DecryptEncryptedKey(EncryptedKey encryptedKey);
         public static byte[] DecryptKey(byte[] keyData, RSA rsa, bool useOAEP);
         public static byte[] DecryptKey(byte[] keyData, SymmetricAlgorithm symmetricAlgorithm);
         public EncryptedData Encrypt(XmlElement inputElement, X509Certificate2 certificate);
         public EncryptedData Encrypt(XmlElement inputElement, string keyName);
         public byte[] EncryptData(byte[] plaintext, SymmetricAlgorithm symmetricAlgorithm);
         public byte[] EncryptData(XmlElement inputElement, SymmetricAlgorithm symmetricAlgorithm, bool content);
         public static byte[] EncryptKey(byte[] keyData, RSA rsa, bool useOAEP);
         public static byte[] EncryptKey(byte[] keyData, SymmetricAlgorithm symmetricAlgorithm);
         public virtual byte[] GetDecryptionIV(EncryptedData encryptedData, string symmetricAlgorithmUri);
         public virtual SymmetricAlgorithm GetDecryptionKey(EncryptedData encryptedData, string symmetricAlgorithmUri);
         public virtual XmlElement GetIdElement(XmlDocument document, string idValue);
         public void ReplaceData(XmlElement inputElement, byte[] decryptedData);
         public static void ReplaceElement(XmlElement inputElement, EncryptedData encryptedData, bool content);
     }
     public class EncryptionMethod {
         public EncryptionMethod();
         public EncryptionMethod(string algorithm);
         public string KeyAlgorithm { get; set; }
         public int KeySize { get; set; }
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public sealed class EncryptionProperty {
         public EncryptionProperty();
         public EncryptionProperty(XmlElement elementProperty);
         public string Id { get; }
         public XmlElement PropertyElement { get; set; }
         public string Target { get; }
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public sealed class EncryptionPropertyCollection : ICollection, IEnumerable, IList {
         public EncryptionPropertyCollection();
         public int Count { get; }
         public bool IsFixedSize { get; }
         public bool IsReadOnly { get; }
         public bool IsSynchronized { get; }
         public object SyncRoot { get; }
         object System.Collections.IList.this[int index] { get; set; }
         [System.Runtime.CompilerServices.IndexerName("ItemOf")]
         public EncryptionProperty this[int index] { get; set; }
         public int Add(EncryptionProperty value);
         public void Clear();
         public bool Contains(EncryptionProperty value);
         public void CopyTo(Array array, int index);
         public void CopyTo(EncryptionProperty[] array, int index);
         public IEnumerator GetEnumerator();
         public int IndexOf(EncryptionProperty value);
         public void Insert(int index, EncryptionProperty value);
         public EncryptionProperty Item(int index);
         public void Remove(EncryptionProperty value);
         public void RemoveAt(int index);
         int System.Collections.IList.Add(object value);
         bool System.Collections.IList.Contains(object value);
         int System.Collections.IList.IndexOf(object value);
         void System.Collections.IList.Insert(int index, object value);
         void System.Collections.IList.Remove(object value);
     }
     public interface IRelDecryptor {
         Stream Decrypt(EncryptionMethod encryptionMethod, KeyInfo keyInfo, Stream toDecrypt);
     }
     public class KeyInfo : IEnumerable {
         public KeyInfo();
         public int Count { get; }
         public string Id { get; set; }
         public void AddClause(KeyInfoClause clause);
         public IEnumerator GetEnumerator();
         public IEnumerator GetEnumerator(Type requestedObjectType);
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public abstract class KeyInfoClause {
         protected KeyInfoClause();
         public abstract XmlElement GetXml();
         public abstract void LoadXml(XmlElement element);
     }
     public class KeyInfoEncryptedKey : KeyInfoClause {
         public KeyInfoEncryptedKey();
         public KeyInfoEncryptedKey(EncryptedKey encryptedKey);
         public EncryptedKey EncryptedKey { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public class KeyInfoName : KeyInfoClause {
         public KeyInfoName();
         public KeyInfoName(string keyName);
         public string Value { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public class KeyInfoNode : KeyInfoClause {
         public KeyInfoNode();
         public KeyInfoNode(XmlElement node);
         public XmlElement Value { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public class KeyInfoRetrievalMethod : KeyInfoClause {
         public KeyInfoRetrievalMethod();
         public KeyInfoRetrievalMethod(string strUri);
         public KeyInfoRetrievalMethod(string strUri, string typeName);
         public string Type { get; set; }
         public string Uri { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public class KeyInfoX509Data : KeyInfoClause {
         public KeyInfoX509Data();
         public KeyInfoX509Data(byte[] rgbCert);
         public KeyInfoX509Data(X509Certificate cert);
         public KeyInfoX509Data(X509Certificate cert, X509IncludeOption includeOption);
         public ArrayList Certificates { get; }
         public byte[] CRL { get; set; }
         public ArrayList IssuerSerials { get; }
         public ArrayList SubjectKeyIds { get; }
         public ArrayList SubjectNames { get; }
         public void AddCertificate(X509Certificate certificate);
         public void AddIssuerSerial(string issuerName, string serialNumber);
         public void AddSubjectKeyId(byte[] subjectKeyId);
         public void AddSubjectKeyId(string subjectKeyId);
         public void AddSubjectName(string subjectName);
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement element);
     }
     public sealed class KeyReference : EncryptedReference {
         public KeyReference();
         public KeyReference(string uri);
         public KeyReference(string uri, TransformChain transformChain);
     }
     public class Reference {
         public Reference();
         public Reference(Stream stream);
         public Reference(string uri);
         public string DigestMethod { get; set; }
         public byte[] DigestValue { get; set; }
         public string Id { get; set; }
         public TransformChain TransformChain { get; set; }
         public string Type { get; set; }
         public string Uri { get; set; }
         public void AddTransform(Transform transform);
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public sealed class ReferenceList : ICollection, IEnumerable, IList {
         public ReferenceList();
         public int Count { get; }
         public bool IsSynchronized { get; }
         public object SyncRoot { get; }
         bool System.Collections.IList.IsFixedSize { get; }
         bool System.Collections.IList.IsReadOnly { get; }
         object System.Collections.IList.this[int index] { get; set; }
         [System.Runtime.CompilerServices.IndexerName("ItemOf")]
         public EncryptedReference this[int index] { get; set; }
         public int Add(object value);
         public void Clear();
         public bool Contains(object value);
         public void CopyTo(Array array, int index);
         public IEnumerator GetEnumerator();
         public int IndexOf(object value);
         public void Insert(int index, object value);
         public EncryptedReference Item(int index);
         public void Remove(object value);
         public void RemoveAt(int index);
     }
     public class RSAKeyValue : KeyInfoClause {
         public RSAKeyValue();
         public RSAKeyValue(RSA key);
         public RSA Key { get; set; }
         public override XmlElement GetXml();
         public override void LoadXml(XmlElement value);
     }
     public class Signature {
         public Signature();
         public string Id { get; set; }
         public KeyInfo KeyInfo { get; set; }
         public IList ObjectList { get; set; }
         public byte[] SignatureValue { get; set; }
         public SignedInfo SignedInfo { get; set; }
         public void AddObject(DataObject dataObject);
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public class SignedInfo : ICollection, IEnumerable {
         public SignedInfo();
         public string CanonicalizationMethod { get; set; }
         public Transform CanonicalizationMethodObject { get; }
         public int Count { get; }
         public string Id { get; set; }
         public bool IsReadOnly { get; }
         public bool IsSynchronized { get; }
         public ArrayList References { get; }
         public string SignatureLength { get; set; }
         public string SignatureMethod { get; set; }
         public object SyncRoot { get; }
         public void AddReference(Reference reference);
         public void CopyTo(Array array, int index);
         public IEnumerator GetEnumerator();
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public class SignedXml {
         protected Signature m_signature;
         protected string m_strSigningKeyName;
         public const string XmlDecryptionTransformUrl = "http://www.w3.org/2002/07/decrypt#XML";
         public const string XmlDsigBase64TransformUrl = "http://www.w3.org/2000/09/xmldsig#base64";
         public const string XmlDsigC14NTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
         public const string XmlDsigC14NWithCommentsTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
         public const string XmlDsigCanonicalizationUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
         public const string XmlDsigCanonicalizationWithCommentsUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
         public const string XmlDsigDSAUrl = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
         public const string XmlDsigEnvelopedSignatureTransformUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
         public const string XmlDsigExcC14NTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";
         public const string XmlDsigExcC14NWithCommentsTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
         public const string XmlDsigHMACSHA1Url = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";
         public const string XmlDsigMinimalCanonicalizationUrl = "http://www.w3.org/2000/09/xmldsig#minimal";
         public const string XmlDsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";
         public const string XmlDsigRSASHA1Url = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
+        public const string XmlDsigRSASHA256Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
+        public const string XmlDsigRSASHA384Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
+        public const string XmlDsigRSASHA512Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
         public const string XmlDsigSHA1Url = "http://www.w3.org/2000/09/xmldsig#sha1";
+        public const string XmlDsigSHA256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
+        public const string XmlDsigSHA384Url = "http://www.w3.org/2001/04/xmldsig-more#sha384";
+        public const string XmlDsigSHA512Url = "http://www.w3.org/2001/04/xmlenc#sha512";
         public const string XmlDsigXPathTransformUrl = "http://www.w3.org/TR/1999/REC-xpath-19991116";
         public const string XmlDsigXsltTransformUrl = "http://www.w3.org/TR/1999/REC-xslt-19991116";
         public const string XmlLicenseTransformUrl = "urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform";
         public SignedXml();
         public SignedXml(XmlDocument document);
         public SignedXml(XmlElement elem);
         public EncryptedXml EncryptedXml { get; set; }
         public KeyInfo KeyInfo { get; set; }
         public XmlResolver Resolver { set; }
         public Collection<string> SafeCanonicalizationMethods { get; }
         public Signature Signature { get; }
         public Func<SignedXml, bool> SignatureFormatValidator { get; set; }
         public string SignatureLength { get; }
         public string SignatureMethod { get; }
         public byte[] SignatureValue { get; }
         public SignedInfo SignedInfo { get; }
         public AsymmetricAlgorithm SigningKey { get; set; }
         public string SigningKeyName { get; set; }
         public void AddObject(DataObject dataObject);
         public void AddReference(Reference reference);
         public bool CheckSignature();
         public bool CheckSignature(AsymmetricAlgorithm key);
         public bool CheckSignature(KeyedHashAlgorithm macAlg);
         public bool CheckSignature(X509Certificate2 certificate, bool verifySignatureOnly);
         public bool CheckSignatureReturningKey(out AsymmetricAlgorithm signingKey);
         public void ComputeSignature();
         public void ComputeSignature(KeyedHashAlgorithm macAlg);
         public virtual XmlElement GetIdElement(XmlDocument document, string idValue);
         protected virtual AsymmetricAlgorithm GetPublicKey();
         public XmlElement GetXml();
         public void LoadXml(XmlElement value);
     }
     public abstract class Transform {
         protected Transform();
         public string Algorithm { get; set; }
         public XmlElement Context { get; set; }
         public abstract Type[] InputTypes { get; }
         public abstract Type[] OutputTypes { get; }
         public Hashtable PropagatedNamespaces { get; }
         public XmlResolver Resolver { internal get; set; }
         public virtual byte[] GetDigestedOutput(HashAlgorithm hash);
         protected abstract XmlNodeList GetInnerXml();
         public abstract object GetOutput();
         public abstract object GetOutput(Type type);
         public XmlElement GetXml();
         public abstract void LoadInnerXml(XmlNodeList nodeList);
         public abstract void LoadInput(object obj);
     }
     public class TransformChain {
         public TransformChain();
         public int Count { get; }
         public Transform this[int index] { get; }
         public void Add(Transform transform);
         public IEnumerator GetEnumerator();
     }
-    public struct X509IssuerSerial {
 {
-        public string IssuerName { get; set; }

-        public string SerialNumber { get; set; }

-    }
     public class XmlDecryptionTransform : Transform {
         public XmlDecryptionTransform();
         public EncryptedXml EncryptedXml { get; set; }
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         public void AddExceptUri(string uri);
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         protected virtual bool IsTargetElement(XmlElement inputElement, string idValue);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigBase64Transform : Transform {
         public XmlDsigBase64Transform();
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigC14NTransform : Transform {
         public XmlDsigC14NTransform();
         public XmlDsigC14NTransform(bool includeComments);
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         public override byte[] GetDigestedOutput(HashAlgorithm hash);
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigC14NWithCommentsTransform : XmlDsigC14NTransform {
         public XmlDsigC14NWithCommentsTransform();
     }
     public class XmlDsigEnvelopedSignatureTransform : Transform {
         public XmlDsigEnvelopedSignatureTransform();
         public XmlDsigEnvelopedSignatureTransform(bool includeComments);
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigExcC14NTransform : Transform {
         public XmlDsigExcC14NTransform();
         public XmlDsigExcC14NTransform(bool includeComments);
         public XmlDsigExcC14NTransform(bool includeComments, string inclusiveNamespacesPrefixList);
         public XmlDsigExcC14NTransform(string inclusiveNamespacesPrefixList);
         public string InclusiveNamespacesPrefixList { get; set; }
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         public override byte[] GetDigestedOutput(HashAlgorithm hash);
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigExcC14NWithCommentsTransform : XmlDsigExcC14NTransform {
         public XmlDsigExcC14NWithCommentsTransform();
         public XmlDsigExcC14NWithCommentsTransform(string inclusiveNamespacesPrefixList);
     }
     public class XmlDsigXPathTransform : Transform {
         public XmlDsigXPathTransform();
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlDsigXsltTransform : Transform {
         public XmlDsigXsltTransform();
         public XmlDsigXsltTransform(bool includeComments);
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
     public class XmlLicenseTransform : Transform {
         public XmlLicenseTransform();
         public IRelDecryptor Decryptor { get; set; }
         public override Type[] InputTypes { get; }
         public override Type[] OutputTypes { get; }
         protected override XmlNodeList GetInnerXml();
         public override object GetOutput();
         public override object GetOutput(Type type);
         public override void LoadInnerXml(XmlNodeList nodeList);
         public override void LoadInput(object obj);
     }
 }
```

