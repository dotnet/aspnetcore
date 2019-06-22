# System.Security.Cryptography.Pkcs

``` diff
-namespace System.Security.Cryptography.Pkcs {
 {
-    public sealed class AlgorithmIdentifier {
 {
-        public AlgorithmIdentifier();

-        public AlgorithmIdentifier(Oid oid);

-        public AlgorithmIdentifier(Oid oid, int keyLength);

-        public int KeyLength { get; set; }

-        public Oid Oid { get; set; }

-    }
-    public sealed class CmsRecipient {
 {
-        public CmsRecipient(SubjectIdentifierType recipientIdentifierType, X509Certificate2 certificate);

-        public CmsRecipient(X509Certificate2 certificate);

-        public X509Certificate2 Certificate { get; }

-        public SubjectIdentifierType RecipientIdentifierType { get; }

-    }
-    public sealed class CmsRecipientCollection : ICollection, IEnumerable {
 {
-        public CmsRecipientCollection();

-        public CmsRecipientCollection(CmsRecipient recipient);

-        public CmsRecipientCollection(SubjectIdentifierType recipientIdentifierType, X509Certificate2Collection certificates);

-        public int Count { get; }

-        bool System.Collections.ICollection.IsSynchronized { get; }

-        object System.Collections.ICollection.SyncRoot { get; }

-        public CmsRecipient this[int index] { get; }

-        public int Add(CmsRecipient recipient);

-        public void CopyTo(Array array, int index);

-        public void CopyTo(CmsRecipient[] array, int index);

-        public CmsRecipientEnumerator GetEnumerator();

-        public void Remove(CmsRecipient recipient);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public sealed class CmsRecipientEnumerator : IEnumerator {
 {
-        public CmsRecipient Current { get; }

-        object System.Collections.IEnumerator.Current { get; }

-        public bool MoveNext();

-        public void Reset();

-    }
-    public sealed class CmsSigner {
 {
-        public CmsSigner();

-        public CmsSigner(CspParameters parameters);

-        public CmsSigner(SubjectIdentifierType signerIdentifierType);

-        public CmsSigner(SubjectIdentifierType signerIdentifierType, X509Certificate2 certificate);

-        public CmsSigner(X509Certificate2 certificate);

-        public X509Certificate2 Certificate { get; set; }

-        public X509Certificate2Collection Certificates { get; set; }

-        public Oid DigestAlgorithm { get; set; }

-        public X509IncludeOption IncludeOption { get; set; }

-        public CryptographicAttributeObjectCollection SignedAttributes { get; set; }

-        public SubjectIdentifierType SignerIdentifierType { get; set; }

-        public CryptographicAttributeObjectCollection UnsignedAttributes { get; set; }

-    }
-    public sealed class ContentInfo {
 {
-        public ContentInfo(byte[] content);

-        public ContentInfo(Oid contentType, byte[] content);

-        public byte[] Content { get; }

-        public Oid ContentType { get; }

-        public static Oid GetContentType(byte[] encodedMessage);

-    }
-    public sealed class EnvelopedCms {
 {
-        public EnvelopedCms();

-        public EnvelopedCms(ContentInfo contentInfo);

-        public EnvelopedCms(ContentInfo contentInfo, AlgorithmIdentifier encryptionAlgorithm);

-        public X509Certificate2Collection Certificates { get; private set; }

-        public AlgorithmIdentifier ContentEncryptionAlgorithm { get; private set; }

-        public ContentInfo ContentInfo { get; private set; }

-        public RecipientInfoCollection RecipientInfos { get; }

-        public CryptographicAttributeObjectCollection UnprotectedAttributes { get; private set; }

-        public int Version { get; private set; }

-        public void Decode(byte[] encodedMessage);

-        public void Decrypt();

-        public void Decrypt(RecipientInfo recipientInfo);

-        public void Decrypt(RecipientInfo recipientInfo, X509Certificate2Collection extraStore);

-        public void Decrypt(X509Certificate2Collection extraStore);

-        public byte[] Encode();

-        public void Encrypt(CmsRecipient recipient);

-        public void Encrypt(CmsRecipientCollection recipients);

-    }
-    public sealed class KeyAgreeRecipientInfo : RecipientInfo {
 {
-        public DateTime Date { get; }

-        public override byte[] EncryptedKey { get; }

-        public override AlgorithmIdentifier KeyEncryptionAlgorithm { get; }

-        public SubjectIdentifierOrKey OriginatorIdentifierOrKey { get; }

-        public CryptographicAttributeObject OtherKeyAttribute { get; }

-        public override SubjectIdentifier RecipientIdentifier { get; }

-        public override int Version { get; }

-    }
-    public sealed class KeyTransRecipientInfo : RecipientInfo {
 {
-        public override byte[] EncryptedKey { get; }

-        public override AlgorithmIdentifier KeyEncryptionAlgorithm { get; }

-        public override SubjectIdentifier RecipientIdentifier { get; }

-        public override int Version { get; }

-    }
-    public class Pkcs9AttributeObject : AsnEncodedData {
 {
-        public Pkcs9AttributeObject();

-        public Pkcs9AttributeObject(AsnEncodedData asnEncodedData);

-        public Pkcs9AttributeObject(Oid oid, byte[] encodedData);

-        public Pkcs9AttributeObject(string oid, byte[] encodedData);

-        public Oid Oid { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class Pkcs9ContentType : Pkcs9AttributeObject {
 {
-        public Pkcs9ContentType();

-        public Oid ContentType { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class Pkcs9DocumentDescription : Pkcs9AttributeObject {
 {
-        public Pkcs9DocumentDescription();

-        public Pkcs9DocumentDescription(byte[] encodedDocumentDescription);

-        public Pkcs9DocumentDescription(string documentDescription);

-        public string DocumentDescription { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class Pkcs9DocumentName : Pkcs9AttributeObject {
 {
-        public Pkcs9DocumentName();

-        public Pkcs9DocumentName(byte[] encodedDocumentName);

-        public Pkcs9DocumentName(string documentName);

-        public string DocumentName { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class Pkcs9MessageDigest : Pkcs9AttributeObject {
 {
-        public Pkcs9MessageDigest();

-        public byte[] MessageDigest { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class Pkcs9SigningTime : Pkcs9AttributeObject {
 {
-        public Pkcs9SigningTime();

-        public Pkcs9SigningTime(byte[] encodedSigningTime);

-        public Pkcs9SigningTime(DateTime signingTime);

-        public DateTime SigningTime { get; }

-        public override void CopyFrom(AsnEncodedData asnEncodedData);

-    }
-    public sealed class PublicKeyInfo {
 {
-        public AlgorithmIdentifier Algorithm { get; }

-        public byte[] KeyValue { get; }

-    }
-    public abstract class RecipientInfo {
 {
-        public abstract byte[] EncryptedKey { get; }

-        public abstract AlgorithmIdentifier KeyEncryptionAlgorithm { get; }

-        public abstract SubjectIdentifier RecipientIdentifier { get; }

-        public RecipientInfoType Type { get; }

-        public abstract int Version { get; }

-    }
-    public sealed class RecipientInfoCollection : ICollection, IEnumerable {
 {
-        public int Count { get; }

-        bool System.Collections.ICollection.IsSynchronized { get; }

-        object System.Collections.ICollection.SyncRoot { get; }

-        public RecipientInfo this[int index] { get; }

-        public void CopyTo(Array array, int index);

-        public void CopyTo(RecipientInfo[] array, int index);

-        public RecipientInfoEnumerator GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public sealed class RecipientInfoEnumerator : IEnumerator {
 {
-        public RecipientInfo Current { get; }

-        object System.Collections.IEnumerator.Current { get; }

-        public bool MoveNext();

-        public void Reset();

-    }
-    public enum RecipientInfoType {
 {
-        KeyAgreement = 2,

-        KeyTransport = 1,

-        Unknown = 0,

-    }
-    public sealed class Rfc3161TimestampRequest {
 {
-        public bool HasExtensions { get; }

-        public Oid HashAlgorithmId { get; }

-        public Oid RequestedPolicyId { get; }

-        public bool RequestSignerCertificate { get; }

-        public int Version { get; }

-        public static Rfc3161TimestampRequest CreateFromData(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, Nullable<ReadOnlyMemory<byte>> nonce = default(Nullable<ReadOnlyMemory<byte>>), bool requestSignerCertificates = false, X509ExtensionCollection extensions = null);

-        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, Nullable<ReadOnlyMemory<byte>> nonce = default(Nullable<ReadOnlyMemory<byte>>), bool requestSignerCertificates = false, X509ExtensionCollection extensions = null);

-        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, Oid hashAlgorithmId, Oid requestedPolicyId = null, Nullable<ReadOnlyMemory<byte>> nonce = default(Nullable<ReadOnlyMemory<byte>>), bool requestSignerCertificates = false, X509ExtensionCollection extensions = null);

-        public static Rfc3161TimestampRequest CreateFromSignerInfo(SignerInfo signerInfo, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, Nullable<ReadOnlyMemory<byte>> nonce = default(Nullable<ReadOnlyMemory<byte>>), bool requestSignerCertificates = false, X509ExtensionCollection extensions = null);

-        public byte[] Encode();

-        public X509ExtensionCollection GetExtensions();

-        public ReadOnlyMemory<byte> GetMessageHash();

-        public Nullable<ReadOnlyMemory<byte>> GetNonce();

-        public Rfc3161TimestampToken ProcessResponse(ReadOnlyMemory<byte> source, out int bytesConsumed);

-        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampRequest request, out int bytesConsumed);

-        public bool TryEncode(Span<byte> destination, out int bytesWritten);

-    }
-    public sealed class Rfc3161TimestampToken {
 {
-        public Rfc3161TimestampTokenInfo TokenInfo { get; private set; }

-        public SignedCms AsSignedCms();

-        public static bool TryDecode(ReadOnlyMemory<byte> source, out Rfc3161TimestampToken token, out int bytesConsumed);

-        public bool VerifySignatureForData(ReadOnlySpan<byte> data, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null);

-        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, HashAlgorithmName hashAlgorithm, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null);

-        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, Oid hashAlgorithmId, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null);

-        public bool VerifySignatureForSignerInfo(SignerInfo signerInfo, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null);

-    }
-    public sealed class Rfc3161TimestampTokenInfo {
 {
-        public Rfc3161TimestampTokenInfo(Oid policyId, Oid hashAlgorithmId, ReadOnlyMemory<byte> messageHash, ReadOnlyMemory<byte> serialNumber, DateTimeOffset timestamp, Nullable<long> accuracyInMicroseconds = default(Nullable<long>), bool isOrdering = false, Nullable<ReadOnlyMemory<byte>> nonce = default(Nullable<ReadOnlyMemory<byte>>), Nullable<ReadOnlyMemory<byte>> tsaName = default(Nullable<ReadOnlyMemory<byte>>), X509ExtensionCollection extensions = null);

-        public Nullable<long> AccuracyInMicroseconds { get; }

-        public bool HasExtensions { get; }

-        public Oid HashAlgorithmId { get; }

-        public bool IsOrdering { get; }

-        public Oid PolicyId { get; }

-        public DateTimeOffset Timestamp { get; }

-        public int Version { get; }

-        public byte[] Encode();

-        public X509ExtensionCollection GetExtensions();

-        public ReadOnlyMemory<byte> GetMessageHash();

-        public Nullable<ReadOnlyMemory<byte>> GetNonce();

-        public ReadOnlyMemory<byte> GetSerialNumber();

-        public Nullable<ReadOnlyMemory<byte>> GetTimestampAuthorityName();

-        public static bool TryDecode(ReadOnlyMemory<byte> source, out Rfc3161TimestampTokenInfo timestampTokenInfo, out int bytesConsumed);

-        public bool TryEncode(Span<byte> destination, out int bytesWritten);

-    }
-    public sealed class SignedCms {
 {
-        public SignedCms();

-        public SignedCms(ContentInfo contentInfo);

-        public SignedCms(ContentInfo contentInfo, bool detached);

-        public SignedCms(SubjectIdentifierType signerIdentifierType);

-        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo);

-        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo, bool detached);

-        public X509Certificate2Collection Certificates { get; }

-        public ContentInfo ContentInfo { get; private set; }

-        public bool Detached { get; private set; }

-        public SignerInfoCollection SignerInfos { get; }

-        public int Version { get; private set; }

-        public void CheckHash();

-        public void CheckSignature(bool verifySignatureOnly);

-        public void CheckSignature(X509Certificate2Collection extraStore, bool verifySignatureOnly);

-        public void ComputeSignature();

-        public void ComputeSignature(CmsSigner signer);

-        public void ComputeSignature(CmsSigner signer, bool silent);

-        public void Decode(byte[] encodedMessage);

-        public byte[] Encode();

-        public void RemoveSignature(int index);

-        public void RemoveSignature(SignerInfo signerInfo);

-    }
-    public sealed class SignerInfo {
 {
-        public X509Certificate2 Certificate { get; }

-        public SignerInfoCollection CounterSignerInfos { get; }

-        public Oid DigestAlgorithm { get; }

-        public Oid SignatureAlgorithm { get; }

-        public CryptographicAttributeObjectCollection SignedAttributes { get; }

-        public SubjectIdentifier SignerIdentifier { get; }

-        public CryptographicAttributeObjectCollection UnsignedAttributes { get; }

-        public int Version { get; }

-        public void CheckHash();

-        public void CheckSignature(bool verifySignatureOnly);

-        public void CheckSignature(X509Certificate2Collection extraStore, bool verifySignatureOnly);

-        public void ComputeCounterSignature();

-        public void ComputeCounterSignature(CmsSigner signer);

-        public byte[] GetSignature();

-        public void RemoveCounterSignature(int index);

-        public void RemoveCounterSignature(SignerInfo counterSignerInfo);

-    }
-    public sealed class SignerInfoCollection : ICollection, IEnumerable {
 {
-        public int Count { get; }

-        public bool IsSynchronized { get; }

-        public object SyncRoot { get; }

-        public SignerInfo this[int index] { get; }

-        public void CopyTo(Array array, int index);

-        public void CopyTo(SignerInfo[] array, int index);

-        public SignerInfoEnumerator GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public sealed class SignerInfoEnumerator : IEnumerator {
 {
-        public SignerInfo Current { get; }

-        object System.Collections.IEnumerator.Current { get; }

-        public bool MoveNext();

-        public void Reset();

-    }
-    public sealed class SubjectIdentifier {
 {
-        public SubjectIdentifierType Type { get; }

-        public object Value { get; }

-    }
-    public sealed class SubjectIdentifierOrKey {
 {
-        public SubjectIdentifierOrKeyType Type { get; }

-        public object Value { get; }

-    }
-    public enum SubjectIdentifierOrKeyType {
 {
-        IssuerAndSerialNumber = 1,

-        PublicKeyInfo = 3,

-        SubjectKeyIdentifier = 2,

-        Unknown = 0,

-    }
-    public enum SubjectIdentifierType {
 {
-        IssuerAndSerialNumber = 1,

-        NoSignature = 3,

-        SubjectKeyIdentifier = 2,

-        Unknown = 0,

-    }
-}
```

