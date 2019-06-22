# System.Security.Cryptography

``` diff
 namespace System.Security.Cryptography {
+    public sealed class AesCng : Aes {
+        public AesCng();
+        public AesCng(string keyName);
+        public AesCng(string keyName, CngProvider provider);
+        public AesCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions);
+        public override byte[] Key { get; set; }
+        public override int KeySize { get; set; }
+        public override ICryptoTransform CreateDecryptor();
+        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
+        public override ICryptoTransform CreateEncryptor();
+        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
+        protected override void Dispose(bool disposing);
+        public override void GenerateIV();
+        public override void GenerateKey();
+    }
+    public sealed class CngAlgorithm : IEquatable<CngAlgorithm> {
+        public CngAlgorithm(string algorithm);
+        public string Algorithm { get; }
+        public static CngAlgorithm ECDiffieHellman { get; }
+        public static CngAlgorithm ECDiffieHellmanP256 { get; }
+        public static CngAlgorithm ECDiffieHellmanP384 { get; }
+        public static CngAlgorithm ECDiffieHellmanP521 { get; }
+        public static CngAlgorithm ECDsa { get; }
+        public static CngAlgorithm ECDsaP256 { get; }
+        public static CngAlgorithm ECDsaP384 { get; }
+        public static CngAlgorithm ECDsaP521 { get; }
+        public static CngAlgorithm MD5 { get; }
+        public static CngAlgorithm Rsa { get; }
+        public static CngAlgorithm Sha1 { get; }
+        public static CngAlgorithm Sha256 { get; }
+        public static CngAlgorithm Sha384 { get; }
+        public static CngAlgorithm Sha512 { get; }
+        public override bool Equals(object obj);
+        public bool Equals(CngAlgorithm other);
+        public override int GetHashCode();
+        public static bool operator ==(CngAlgorithm left, CngAlgorithm right);
+        public static bool operator !=(CngAlgorithm left, CngAlgorithm right);
+        public override string ToString();
+    }
+    public sealed class CngAlgorithmGroup : IEquatable<CngAlgorithmGroup> {
+        public CngAlgorithmGroup(string algorithmGroup);
+        public string AlgorithmGroup { get; }
+        public static CngAlgorithmGroup DiffieHellman { get; }
+        public static CngAlgorithmGroup Dsa { get; }
+        public static CngAlgorithmGroup ECDiffieHellman { get; }
+        public static CngAlgorithmGroup ECDsa { get; }
+        public static CngAlgorithmGroup Rsa { get; }
+        public override bool Equals(object obj);
+        public bool Equals(CngAlgorithmGroup other);
+        public override int GetHashCode();
+        public static bool operator ==(CngAlgorithmGroup left, CngAlgorithmGroup right);
+        public static bool operator !=(CngAlgorithmGroup left, CngAlgorithmGroup right);
+        public override string ToString();
+    }
+    public enum CngExportPolicies {
+        AllowArchiving = 4,
+        AllowExport = 1,
+        AllowPlaintextArchiving = 8,
+        AllowPlaintextExport = 2,
+        None = 0,
+    }
+    public sealed class CngKey : IDisposable {
+        public CngAlgorithm Algorithm { get; }
+        public CngAlgorithmGroup AlgorithmGroup { get; }
+        public CngExportPolicies ExportPolicy { get; }
+        public SafeNCryptKeyHandle Handle { get; }
+        public bool IsEphemeral { get; }
+        public bool IsMachineKey { get; }
+        public string KeyName { get; }
+        public int KeySize { get; }
+        public CngKeyUsages KeyUsage { get; }
+        public IntPtr ParentWindowHandle { get; set; }
+        public CngProvider Provider { get; }
+        public SafeNCryptProviderHandle ProviderHandle { get; }
+        public CngUIPolicy UIPolicy { get; }
+        public string UniqueName { get; }
+        public static CngKey Create(CngAlgorithm algorithm);
+        public static CngKey Create(CngAlgorithm algorithm, string keyName);
+        public static CngKey Create(CngAlgorithm algorithm, string keyName, CngKeyCreationParameters creationParameters);
+        public void Delete();
+        public void Dispose();
+        public static bool Exists(string keyName);
+        public static bool Exists(string keyName, CngProvider provider);
+        public static bool Exists(string keyName, CngProvider provider, CngKeyOpenOptions options);
+        public byte[] Export(CngKeyBlobFormat format);
+        public CngProperty GetProperty(string name, CngPropertyOptions options);
+        public bool HasProperty(string name, CngPropertyOptions options);
+        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format);
+        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider);
+        public static CngKey Open(SafeNCryptKeyHandle keyHandle, CngKeyHandleOpenOptions keyHandleOpenOptions);
+        public static CngKey Open(string keyName);
+        public static CngKey Open(string keyName, CngProvider provider);
+        public static CngKey Open(string keyName, CngProvider provider, CngKeyOpenOptions openOptions);
+        public void SetProperty(CngProperty property);
+    }
+    public sealed class CngKeyBlobFormat : IEquatable<CngKeyBlobFormat> {
+        public CngKeyBlobFormat(string format);
+        public static CngKeyBlobFormat EccFullPrivateBlob { get; }
+        public static CngKeyBlobFormat EccFullPublicBlob { get; }
+        public static CngKeyBlobFormat EccPrivateBlob { get; }
+        public static CngKeyBlobFormat EccPublicBlob { get; }
+        public string Format { get; }
+        public static CngKeyBlobFormat GenericPrivateBlob { get; }
+        public static CngKeyBlobFormat GenericPublicBlob { get; }
+        public static CngKeyBlobFormat OpaqueTransportBlob { get; }
+        public static CngKeyBlobFormat Pkcs8PrivateBlob { get; }
+        public override bool Equals(object obj);
+        public bool Equals(CngKeyBlobFormat other);
+        public override int GetHashCode();
+        public static bool operator ==(CngKeyBlobFormat left, CngKeyBlobFormat right);
+        public static bool operator !=(CngKeyBlobFormat left, CngKeyBlobFormat right);
+        public override string ToString();
+    }
+    public enum CngKeyCreationOptions {
+        MachineKey = 32,
+        None = 0,
+        OverwriteExistingKey = 128,
+    }
+    public sealed class CngKeyCreationParameters {
+        public CngKeyCreationParameters();
+        public Nullable<CngExportPolicies> ExportPolicy { get; set; }
+        public CngKeyCreationOptions KeyCreationOptions { get; set; }
+        public Nullable<CngKeyUsages> KeyUsage { get; set; }
+        public CngPropertyCollection Parameters { get; }
+        public IntPtr ParentWindowHandle { get; set; }
+        public CngProvider Provider { get; set; }
+        public CngUIPolicy UIPolicy { get; set; }
+    }
+    public enum CngKeyHandleOpenOptions {
+        EphemeralKey = 1,
+        None = 0,
+    }
+    public enum CngKeyOpenOptions {
+        MachineKey = 32,
+        None = 0,
+        Silent = 64,
+        UserKey = 0,
+    }
+    public enum CngKeyUsages {
+        AllUsages = 16777215,
+        Decryption = 1,
+        KeyAgreement = 4,
+        None = 0,
+        Signing = 2,
+    }
+    public struct CngProperty : IEquatable<CngProperty> {
+        public CngProperty(string name, byte[] value, CngPropertyOptions options);
+        public string Name { get; }
+        public CngPropertyOptions Options { get; }
+        public override bool Equals(object obj);
+        public bool Equals(CngProperty other);
+        public override int GetHashCode();
+        public byte[] GetValue();
+        public static bool operator ==(CngProperty left, CngProperty right);
+        public static bool operator !=(CngProperty left, CngProperty right);
+    }
+    public sealed class CngPropertyCollection : Collection<CngProperty> {
+        public CngPropertyCollection();
+    }
+    public enum CngPropertyOptions {
+        CustomProperty = 1073741824,
+        None = 0,
+        Persist = -2147483648,
+    }
+    public sealed class CngProvider : IEquatable<CngProvider> {
+        public CngProvider(string provider);
+        public static CngProvider MicrosoftSmartCardKeyStorageProvider { get; }
+        public static CngProvider MicrosoftSoftwareKeyStorageProvider { get; }
+        public string Provider { get; }
+        public override bool Equals(object obj);
+        public bool Equals(CngProvider other);
+        public override int GetHashCode();
+        public static bool operator ==(CngProvider left, CngProvider right);
+        public static bool operator !=(CngProvider left, CngProvider right);
+        public override string ToString();
+    }
+    public sealed class CngUIPolicy {
+        public CngUIPolicy(CngUIProtectionLevels protectionLevel);
+        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName);
+        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description);
+        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext);
+        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext, string creationTitle);
+        public string CreationTitle { get; }
+        public string Description { get; }
+        public string FriendlyName { get; }
+        public CngUIProtectionLevels ProtectionLevel { get; }
+        public string UseContext { get; }
+    }
+    public enum CngUIProtectionLevels {
+        ForceHighProtection = 2,
+        None = 0,
+        ProtectKey = 1,
+    }
-    public sealed class CryptographicAttributeObject {
 {
-        public CryptographicAttributeObject(Oid oid);

-        public CryptographicAttributeObject(Oid oid, AsnEncodedDataCollection values);

-        public Oid Oid { get; }

-        public AsnEncodedDataCollection Values { get; }

-    }
-    public sealed class CryptographicAttributeObjectCollection : ICollection, IEnumerable {
 {
-        public CryptographicAttributeObjectCollection();

-        public CryptographicAttributeObjectCollection(CryptographicAttributeObject attribute);

-        public int Count { get; }

-        bool System.Collections.ICollection.IsSynchronized { get; }

-        object System.Collections.ICollection.SyncRoot { get; }

-        public CryptographicAttributeObject this[int index] { get; }

-        public int Add(AsnEncodedData asnEncodedData);

-        public int Add(CryptographicAttributeObject attribute);

-        public void CopyTo(CryptographicAttributeObject[] array, int index);

-        public CryptographicAttributeObjectEnumerator GetEnumerator();

-        public void Remove(CryptographicAttributeObject attribute);

-        void System.Collections.ICollection.CopyTo(Array array, int index);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public sealed class CryptographicAttributeObjectEnumerator : IEnumerator {
 {
-        public CryptographicAttributeObject Current { get; }

-        object System.Collections.IEnumerator.Current { get; }

-        public bool MoveNext();

-        public void Reset();

-    }
+    public sealed class DSACng : DSA {
+        public DSACng();
+        public DSACng(int keySize);
+        public DSACng(CngKey key);
+        public CngKey Key { get; }
+        public override string KeyExchangeAlgorithm { get; }
+        public override KeySizes[] LegalKeySizes { get; }
+        public override string SignatureAlgorithm { get; }
+        public override byte[] CreateSignature(byte[] rgbHash);
+        protected override void Dispose(bool disposing);
+        public override DSAParameters ExportParameters(bool includePrivateParameters);
+        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);
+        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm);
+        public override void ImportParameters(DSAParameters parameters);
+        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
+    }
+    public sealed class ECDiffieHellmanCng : ECDiffieHellman {
+        public ECDiffieHellmanCng();
+        public ECDiffieHellmanCng(int keySize);
+        public ECDiffieHellmanCng(CngKey key);
+        public ECDiffieHellmanCng(ECCurve curve);
+        public CngAlgorithm HashAlgorithm { get; set; }
+        public byte[] HmacKey { get; set; }
+        public CngKey Key { get; }
+        public ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction { get; set; }
+        public override int KeySize { get; set; }
+        public byte[] Label { get; set; }
+        public override ECDiffieHellmanPublicKey PublicKey { get; }
+        public byte[] SecretAppend { get; set; }
+        public byte[] SecretPrepend { get; set; }
+        public byte[] Seed { get; set; }
+        public bool UseSecretAgreementAsHmacKey { get; }
+        public override byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] secretPrepend, byte[] secretAppend);
+        public override byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] hmacKey, byte[] secretPrepend, byte[] secretAppend);
+        public byte[] DeriveKeyMaterial(CngKey otherPartyPublicKey);
+        public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey);
+        public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed);
+        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(CngKey otherPartyPublicKey);
+        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey);
+        protected override void Dispose(bool disposing);
+        public override ECParameters ExportExplicitParameters(bool includePrivateParameters);
+        public override ECParameters ExportParameters(bool includePrivateParameters);
+        public void FromXmlString(string xml, ECKeyXmlFormat format);
+        public override void GenerateKey(ECCurve curve);
+        public override void ImportParameters(ECParameters parameters);
+        public string ToXmlString(ECKeyXmlFormat format);
+    }
+    public sealed class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey {
+        public CngKeyBlobFormat BlobFormat { get; }
+        protected override void Dispose(bool disposing);
+        public override ECParameters ExportExplicitParameters();
+        public override ECParameters ExportParameters();
+        public static ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, CngKeyBlobFormat format);
+        public static ECDiffieHellmanCngPublicKey FromXmlString(string xml);
+        public CngKey Import();
+        public override string ToXmlString();
+    }
+    public enum ECDiffieHellmanKeyDerivationFunction {
+        Hash = 0,
+        Hmac = 1,
+        Tls = 2,
+    }
+    public sealed class ECDsaCng : ECDsa {
+        public ECDsaCng();
+        public ECDsaCng(int keySize);
+        public ECDsaCng(CngKey key);
+        public ECDsaCng(ECCurve curve);
+        public CngAlgorithm HashAlgorithm { get; set; }
+        public CngKey Key { get; }
+        public override int KeySize { get; set; }
+        public override KeySizes[] LegalKeySizes { get; }
+        protected override void Dispose(bool disposing);
+        public override ECParameters ExportExplicitParameters(bool includePrivateParameters);
+        public override ECParameters ExportParameters(bool includePrivateParameters);
+        public void FromXmlString(string xml, ECKeyXmlFormat format);
+        public override void GenerateKey(ECCurve curve);
+        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);
+        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm);
+        public override void ImportParameters(ECParameters parameters);
+        public byte[] SignData(byte[] data);
+        public byte[] SignData(byte[] data, int offset, int count);
+        public byte[] SignData(Stream data);
+        public override byte[] SignHash(byte[] hash);
+        public string ToXmlString(ECKeyXmlFormat format);
+        public bool VerifyData(byte[] data, byte[] signature);
+        public bool VerifyData(byte[] data, int offset, int count, byte[] signature);
+        public bool VerifyData(Stream data, byte[] signature);
+        public override bool VerifyHash(byte[] hash, byte[] signature);
+    }
+    public enum ECKeyXmlFormat {
+        Rfc4050 = 0,
+    }
+    public sealed class RSACng : RSA {
+        public RSACng();
+        public RSACng(int keySize);
+        public RSACng(CngKey key);
+        public CngKey Key { get; }
+        public override KeySizes[] LegalKeySizes { get; }
+        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding);
+        protected override void Dispose(bool disposing);
+        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding);
+        public override RSAParameters ExportParameters(bool includePrivateParameters);
+        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);
+        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm);
+        public override void ImportParameters(RSAParameters parameters);
+        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
+        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
+    }
+    public sealed class TripleDESCng : TripleDES {
+        public TripleDESCng();
+        public TripleDESCng(string keyName);
+        public TripleDESCng(string keyName, CngProvider provider);
+        public TripleDESCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions);
+        public override byte[] Key { get; set; }
+        public override int KeySize { get; set; }
+        public override ICryptoTransform CreateDecryptor();
+        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
+        public override ICryptoTransform CreateEncryptor();
+        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
+        protected override void Dispose(bool disposing);
+        public override void GenerateIV();
+        public override void GenerateKey();
+    }
 }
```

