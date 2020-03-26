// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.DataProtection
{
    internal static partial class ActivatorExtensions
    {
        public static T CreateInstance<T>(this Microsoft.AspNetCore.DataProtection.Internal.IActivator activator, string implementationTypeName) where T : class { throw null; }
        public static Microsoft.AspNetCore.DataProtection.Internal.IActivator GetActivator(this System.IServiceProvider serviceProvider) { throw null; }
    }
    internal static partial class ArraySegmentExtensions
    {
        public static byte[] AsStandaloneArray(this System.ArraySegment<byte> arraySegment) { throw null; }
        public static void Validate<T>(this System.ArraySegment<T> arraySegment) { }
    }
    internal partial interface IRegistryPolicyResolver
    {
        Microsoft.AspNetCore.DataProtection.RegistryPolicy ResolvePolicy();
    }
    internal sealed partial class RegistryPolicyResolver : Microsoft.AspNetCore.DataProtection.IRegistryPolicyResolver
    {
        public RegistryPolicyResolver(Microsoft.AspNetCore.DataProtection.Internal.IActivator activator) { }
        internal RegistryPolicyResolver(Microsoft.Win32.RegistryKey policyRegKey, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator) { }
        public Microsoft.AspNetCore.DataProtection.RegistryPolicy ResolvePolicy() { throw null; }
    }
    internal static partial class Error
    {
        public static System.InvalidOperationException CertificateXmlEncryptor_CertificateNotFound(string thumbprint) { throw null; }
        public static System.ArgumentException Common_ArgumentCannotBeNullOrEmpty(string parameterName) { throw null; }
        public static System.ArgumentException Common_BufferIncorrectlySized(string parameterName, int actualSize, int expectedSize) { throw null; }
        public static System.Security.Cryptography.CryptographicException Common_EncryptionFailed(System.Exception inner = null) { throw null; }
        public static System.Security.Cryptography.CryptographicException Common_KeyNotFound(System.Guid id) { throw null; }
        public static System.Security.Cryptography.CryptographicException Common_KeyRevoked(System.Guid id) { throw null; }
        public static System.InvalidOperationException Common_PropertyCannotBeNullOrEmpty(string propertyName) { throw null; }
        public static System.InvalidOperationException Common_PropertyMustBeNonNegative(string propertyName) { throw null; }
        public static System.ArgumentOutOfRangeException Common_ValueMustBeNonNegative(string paramName) { throw null; }
        public static System.Security.Cryptography.CryptographicException CryptCommon_GenericError(System.Exception inner = null) { throw null; }
        public static System.Security.Cryptography.CryptographicException CryptCommon_PayloadInvalid() { throw null; }
        public static System.Security.Cryptography.CryptographicException DecryptionFailed(System.Exception inner) { throw null; }
        public static System.Security.Cryptography.CryptographicException ProtectionProvider_BadMagicHeader() { throw null; }
        public static System.Security.Cryptography.CryptographicException ProtectionProvider_BadVersion() { throw null; }
        public static System.InvalidOperationException XmlKeyManager_DuplicateKey(System.Guid keyId) { throw null; }
    }
    internal static partial class Resources
    {
        internal static string AlgorithmAssert_BadBlockSize { get { throw null; } }
        internal static string AlgorithmAssert_BadDigestSize { get { throw null; } }
        internal static string AlgorithmAssert_BadKeySize { get { throw null; } }
        internal static string CertificateXmlEncryptor_CertificateNotFound { get { throw null; } }
        internal static string Common_ArgumentCannotBeNullOrEmpty { get { throw null; } }
        internal static string Common_BufferIncorrectlySized { get { throw null; } }
        internal static string Common_DecryptionFailed { get { throw null; } }
        internal static string Common_EncryptionFailed { get { throw null; } }
        internal static string Common_KeyNotFound { get { throw null; } }
        internal static string Common_KeyRevoked { get { throw null; } }
        internal static string Common_PropertyCannotBeNullOrEmpty { get { throw null; } }
        internal static string Common_PropertyMustBeNonNegative { get { throw null; } }
        internal static string Common_ValueMustBeNonNegative { get { throw null; } }
        internal static string CryptCommon_GenericError { get { throw null; } }
        internal static string CryptCommon_PayloadInvalid { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string EncryptedXmlDecryptor_DoesNotWorkOnCoreClr { get { throw null; } }
        internal static string FileSystem_EphemeralKeysLocationInContainer { get { throw null; } }
        internal static string KeyManagementOptions_MinNewKeyLifetimeViolated { get { throw null; } }
        internal static string KeyRingProvider_NoDefaultKey_AutoGenerateDisabled { get { throw null; } }
        internal static string LifetimeMustNotBeNegative { get { throw null; } }
        internal static string Platform_WindowsRequiredForGcm { get { throw null; } }
        internal static string ProtectionProvider_BadMagicHeader { get { throw null; } }
        internal static string ProtectionProvider_BadVersion { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string TypeExtensions_BadCast { get { throw null; } }
        internal static string XmlKeyManager_DuplicateKey { get { throw null; } }
        internal static string XmlKeyManager_IXmlRepositoryNotFound { get { throw null; } }
        internal static string FormatAlgorithmAssert_BadBlockSize(object p0) { throw null; }
        internal static string FormatAlgorithmAssert_BadDigestSize(object p0) { throw null; }
        internal static string FormatAlgorithmAssert_BadKeySize(object p0) { throw null; }
        internal static string FormatCertificateXmlEncryptor_CertificateNotFound(object p0) { throw null; }
        internal static string FormatCommon_BufferIncorrectlySized(object p0, object p1) { throw null; }
        internal static string FormatCommon_PropertyCannotBeNullOrEmpty(object p0) { throw null; }
        internal static string FormatCommon_PropertyMustBeNonNegative(object p0) { throw null; }
        internal static string FormatFileSystem_EphemeralKeysLocationInContainer(object path) { throw null; }
        internal static string FormatLifetimeMustNotBeNegative(object p0) { throw null; }
        internal static string FormatTypeExtensions_BadCast(object p0, object p1) { throw null; }
        internal static string FormatXmlKeyManager_IXmlRepositoryNotFound(object p0, object p1) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
    }
    internal partial class RegistryPolicy
    {
        public RegistryPolicy(Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration configuration, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink> keyEscrowSinks, int? defaultKeyLifetime) { }
        public int? DefaultKeyLifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration EncryptorConfiguration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink> KeyEscrowSinks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class SimpleActivator : Microsoft.AspNetCore.DataProtection.Internal.IActivator
    {
        internal static readonly Microsoft.AspNetCore.DataProtection.SimpleActivator DefaultWithoutServices;
        public SimpleActivator(System.IServiceProvider services) { }
        public virtual object CreateInstance(System.Type expectedBaseType, string implementationTypeName) { throw null; }
    }
    internal partial class TypeForwardingActivator : Microsoft.AspNetCore.DataProtection.SimpleActivator
    {
        public TypeForwardingActivator(System.IServiceProvider services) : base (default(System.IServiceProvider)) { }
        public TypeForwardingActivator(System.IServiceProvider services, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(System.IServiceProvider)) { }
        public override object CreateInstance(System.Type expectedBaseType, string originalTypeName) { throw null; }
        internal object CreateInstance(System.Type expectedBaseType, string originalTypeName, out bool forwarded) { throw null; }
        protected string RemoveVersionFromAssemblyName(string forwardedTypeName) { throw null; }
    }
    internal static partial class XmlConstants
    {
        internal static readonly System.Xml.Linq.XName DecryptorTypeAttributeName;
        internal static readonly System.Xml.Linq.XName DeserializerTypeAttributeName;
        internal static readonly System.Xml.Linq.XName EncryptedSecretElementName;
        internal static readonly System.Xml.Linq.XName RequiresEncryptionAttributeName;
    }
    internal static partial class XmlExtensions
    {
        public static System.Xml.Linq.XElement WithoutChildNodes(this System.Xml.Linq.XElement element) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal partial class KeyManagementOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>
    {
        public KeyManagementOptionsSetup() { }
        public KeyManagementOptionsSetup(Microsoft.AspNetCore.DataProtection.IRegistryPolicyResolver registryPolicyResolver) { }
        public KeyManagementOptionsSetup(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public KeyManagementOptionsSetup(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.DataProtection.IRegistryPolicyResolver registryPolicyResolver) { }
        public void Configure(Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions options) { }
    }
    internal static partial class ContainerUtils
    {
        public static bool IsContainer { get { throw null; } }
        internal static bool IsDirectoryMounted(System.IO.DirectoryInfo directory, System.Collections.Generic.IEnumerable<string> fstab) { throw null; }
        public static bool IsVolumeMountedFolder(System.IO.DirectoryInfo directory) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    internal partial interface IOptimizedAuthenticatedEncryptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor
    {
        byte[] Encrypt(System.ArraySegment<byte> plaintext, System.ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize);
    }
}
namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public sealed partial class ManagedAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        internal Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.ManagedAuthenticatedEncryptorConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.DataProtection.ISecret MasterKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class CngCbcAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        internal Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngCbcAuthenticatedEncryptorConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.DataProtection.ISecret MasterKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class CngGcmAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        internal Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngGcmAuthenticatedEncryptorConfiguration Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.DataProtection.ISecret MasterKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal static partial class SecretExtensions
    {
        public static System.Xml.Linq.XElement ToMasterKeyElement(this Microsoft.AspNetCore.DataProtection.ISecret secret) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.Secret ToSecret(this string base64String) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.Cng
{
    internal sealed partial class GcmAuthenticatedEncryptor : Microsoft.AspNetCore.DataProtection.Cng.Internal.CngAuthenticatedEncryptorBase
    {
        public GcmAuthenticatedEncryptor(Microsoft.AspNetCore.DataProtection.Secret keyDerivationKey, Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle symmetricAlgorithmHandle, uint symmetricAlgorithmKeySizeInBytes, Microsoft.AspNetCore.DataProtection.Cng.IBCryptGenRandom genRandom = null) { }
        protected unsafe override byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData) { throw null; }
        public override void Dispose() { }
        protected unsafe override byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer) { throw null; }
    }
    internal sealed partial class CbcAuthenticatedEncryptor : Microsoft.AspNetCore.DataProtection.Cng.Internal.CngAuthenticatedEncryptorBase
    {
        public CbcAuthenticatedEncryptor(Microsoft.AspNetCore.DataProtection.Secret keyDerivationKey, Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle symmetricAlgorithmHandle, uint symmetricAlgorithmKeySizeInBytes, Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle hmacAlgorithmHandle, Microsoft.AspNetCore.DataProtection.Cng.IBCryptGenRandom genRandom = null) { }
        protected unsafe override byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData) { throw null; }
        public override void Dispose() { }
        protected unsafe override byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer) { throw null; }
    }
    internal unsafe partial interface IBCryptGenRandom
    {
        void GenRandom(byte* pbBuffer, uint cbBuffer);
    }
}
namespace Microsoft.AspNetCore.DataProtection.Cng.Internal
{
    internal unsafe abstract partial class CngAuthenticatedEncryptorBase : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IOptimizedAuthenticatedEncryptor, System.IDisposable
    {
        protected CngAuthenticatedEncryptorBase() { }
        public byte[] Decrypt(System.ArraySegment<byte> ciphertext, System.ArraySegment<byte> additionalAuthenticatedData) { throw null; }
        protected unsafe abstract byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData);
        public abstract void Dispose();
        public byte[] Encrypt(System.ArraySegment<byte> plaintext, System.ArraySegment<byte> additionalAuthenticatedData) { throw null; }
        public byte[] Encrypt(System.ArraySegment<byte> plaintext, System.ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize) { throw null; }
        protected unsafe abstract byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer);
    }
}
namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    internal static partial class KeyEscrowServiceProviderExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink GetKeyEscrowSink(this System.IServiceProvider services) { throw null; }
    }
    internal sealed partial class DefaultKeyResolver : Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IDefaultKeyResolver
    {
        public DefaultKeyResolver(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions) { }
        public DefaultKeyResolver(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.DefaultKeyResolution ResolveDefaultKeyPolicy(System.DateTimeOffset now, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> allKeys) { throw null; }
    }
    internal sealed partial class DeferredKey : Microsoft.AspNetCore.DataProtection.KeyManagement.KeyBase
    {
        public DeferredKey(System.Guid keyId, System.DateTimeOffset creationDate, System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager keyManager, System.Xml.Linq.XElement keyElement, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory> encryptorFactories) : base (default(System.Guid), default(System.DateTimeOffset), default(System.DateTimeOffset), default(System.DateTimeOffset), default(System.Lazy<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor>), default(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory>)) { }
    }
    internal sealed partial class Key : Microsoft.AspNetCore.DataProtection.KeyManagement.KeyBase
    {
        public Key(System.Guid keyId, System.DateTimeOffset creationDate, System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor descriptor, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory> encryptorFactories) : base (default(System.Guid), default(System.DateTimeOffset), default(System.DateTimeOffset), default(System.DateTimeOffset), default(System.Lazy<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor>), default(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory>)) { }
    }
    internal abstract partial class KeyBase : Microsoft.AspNetCore.DataProtection.KeyManagement.IKey
    {
        public KeyBase(System.Guid keyId, System.DateTimeOffset creationDate, System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate, System.Lazy<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor> lazyDescriptor, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory> encryptorFactories) { }
        public System.DateTimeOffset ActivationDate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.DateTimeOffset CreationDate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor Descriptor { get { throw null; } }
        public System.DateTimeOffset ExpirationDate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsRevoked { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Guid KeyId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptor() { throw null; }
        internal void SetRevoked() { }
    }
    internal sealed partial class KeyRing : Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing
    {
        public KeyRing(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey defaultKey, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> allKeys) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get { throw null; } }
        public System.Guid DefaultKeyId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(System.Guid keyId, out bool isRevoked) { throw null; }
    }
    internal sealed partial class KeyRingProvider : Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.ICacheableKeyRingProvider, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRingProvider
    {
        public KeyRingProvider(Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager keyManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IDefaultKeyResolver defaultKeyResolver) { }
        public KeyRingProvider(Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager keyManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IDefaultKeyResolver defaultKeyResolver, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        internal System.DateTime AutoRefreshWindowEnd { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.ICacheableKeyRingProvider CacheableKeyRingProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing GetCurrentKeyRing() { throw null; }
        internal Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing GetCurrentKeyRingCore(System.DateTime utcNow, bool forceRefresh = false) { throw null; }
        internal bool InAutoRefreshWindow() { throw null; }
        Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.CacheableKeyRing Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.ICacheableKeyRingProvider.GetCacheableKeyRing(System.DateTimeOffset now) { throw null; }
        internal Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing RefreshCurrentKeyRing() { throw null; }
    }
    internal sealed partial class KeyRingBasedDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider, Microsoft.AspNetCore.DataProtection.IDataProtector, Microsoft.AspNetCore.DataProtection.IPersistedDataProtector
    {
        public KeyRingBasedDataProtector(Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRingProvider keyRingProvider, Microsoft.Extensions.Logging.ILogger logger, string[] originalPurposes, string newPurpose) { }
        internal string[] Purposes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) { throw null; }
        public byte[] DangerousUnprotect(byte[] protectedData, bool ignoreRevocationErrors, out bool requiresMigration, out bool wasRevoked) { throw null; }
        public byte[] Protect(byte[] plaintext) { throw null; }
        public byte[] Unprotect(byte[] protectedData) { throw null; }
    }
    public sealed partial class XmlKeyManager : Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager
    {
        internal static readonly System.Xml.Linq.XName ActivationDateElementName;
        internal static readonly System.Xml.Linq.XName CreationDateElementName;
        internal static readonly System.Xml.Linq.XName DescriptorElementName;
        internal static readonly System.Xml.Linq.XName DeserializerTypeAttributeName;
        internal static readonly System.Xml.Linq.XName ExpirationDateElementName;
        internal static readonly System.Xml.Linq.XName IdAttributeName;
        internal static readonly System.Xml.Linq.XName KeyElementName;
        internal static readonly System.Xml.Linq.XName ReasonElementName;
        internal static readonly System.Xml.Linq.XName RevocationDateElementName;
        internal static readonly System.Xml.Linq.XName RevocationElementName;
        internal static readonly System.Xml.Linq.XName VersionAttributeName;
        internal XmlKeyManager(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager internalXmlKeyManager) { }
        internal XmlKeyManager(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.DataProtection.Repositories.IDefaultKeyStorageDirectories keyStorageDirectories) { }
        internal Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor KeyEncryptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository KeyRepository { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal System.Collections.Generic.KeyValuePair<Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository, Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor> GetFallbackKeyRepositoryEncryptorPair() { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal
{
    public sealed partial class CacheableKeyRing
    {
        internal CacheableKeyRing(System.Threading.CancellationToken expirationToken, System.DateTimeOffset expirationTime, Microsoft.AspNetCore.DataProtection.KeyManagement.IKey defaultKey, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> allKeys) { }
        internal CacheableKeyRing(System.Threading.CancellationToken expirationToken, System.DateTimeOffset expirationTime, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing keyRing) { }
        internal System.DateTime ExpirationTimeUtc { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing KeyRing { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal static bool IsValid(Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.CacheableKeyRing keyRing, System.DateTime utcNow) { throw null; }
        internal Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.CacheableKeyRing WithTemporaryExtendedLifetime(System.DateTimeOffset now) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.Managed
{
    internal sealed partial class ManagedAuthenticatedEncryptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor, System.IDisposable
    {
        public ManagedAuthenticatedEncryptor(Microsoft.AspNetCore.DataProtection.Secret keyDerivationKey, System.Func<System.Security.Cryptography.SymmetricAlgorithm> symmetricAlgorithmFactory, int symmetricAlgorithmKeySizeInBytes, System.Func<System.Security.Cryptography.KeyedHashAlgorithm> validationAlgorithmFactory, Microsoft.AspNetCore.DataProtection.Managed.IManagedGenRandom genRandom = null) { }
        public byte[] Decrypt(System.ArraySegment<byte> protectedPayload, System.ArraySegment<byte> additionalAuthenticatedData) { throw null; }
        public void Dispose() { }
        public byte[] Encrypt(System.ArraySegment<byte> plaintext, System.ArraySegment<byte> additionalAuthenticatedData) { throw null; }
    }
    internal partial interface IManagedGenRandom
    {
        byte[] GenRandom(int numBytes);
    }
}
namespace Microsoft.AspNetCore.DataProtection.Repositories
{
    internal partial class EphemeralXmlRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        public EphemeralXmlRepository(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public virtual System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public virtual void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
    internal partial interface IDefaultKeyStorageDirectories
    {
        System.IO.DirectoryInfo GetKeyStorageDirectory();
        System.IO.DirectoryInfo GetKeyStorageDirectoryForAzureWebSites();
    }
}
namespace Microsoft.AspNetCore.DataProtection.SP800_108
{
    internal unsafe partial interface ISP800_108_CTR_HMACSHA512Provider : System.IDisposable
    {
        void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey);
    }
    internal static partial class ManagedSP800_108_CTR_HMACSHA512
    {
        public static void DeriveKeys(byte[] kdk, System.ArraySegment<byte> label, System.ArraySegment<byte> context, System.Func<byte[], System.Security.Cryptography.HashAlgorithm> prfFactory, System.ArraySegment<byte> output) { }
        public static void DeriveKeysWithContextHeader(byte[] kdk, System.ArraySegment<byte> label, byte[] contextHeader, System.ArraySegment<byte> context, System.Func<byte[], System.Security.Cryptography.HashAlgorithm> prfFactory, System.ArraySegment<byte> output) { }
    }
    internal static partial class SP800_108_CTR_HMACSHA512Extensions
    {
        public unsafe static void DeriveKeyWithContextHeader(this Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider provider, byte* pbLabel, uint cbLabel, byte[] contextHeader, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey) { }
    }
    internal static partial class SP800_108_CTR_HMACSHA512Util
    {
        public static Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider CreateEmptyProvider() { throw null; }
        public static Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider CreateProvider(Microsoft.AspNetCore.DataProtection.Secret kdk) { throw null; }
        public unsafe static Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider CreateProvider(byte* pbKdk, uint cbKdk) { throw null; }
    }
    internal sealed partial class Win7SP800_108_CTR_HMACSHA512Provider : Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider, System.IDisposable
    {
        public unsafe Win7SP800_108_CTR_HMACSHA512Provider(byte* pbKdk, uint cbKdk) { }
        public unsafe void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey) { }
        public void Dispose() { }
    }
    internal sealed partial class Win8SP800_108_CTR_HMACSHA512Provider : Microsoft.AspNetCore.DataProtection.SP800_108.ISP800_108_CTR_HMACSHA512Provider, System.IDisposable
    {
        public unsafe Win8SP800_108_CTR_HMACSHA512Provider(byte* pbKdk, uint cbKdk) { }
        public unsafe void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey) { }
        public void Dispose() { }
    }
}
namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    public sealed partial class CertificateXmlEncryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IInternalCertificateXmlEncryptor, Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor
    {
        System.Security.Cryptography.Xml.EncryptedData Microsoft.AspNetCore.DataProtection.XmlEncryption.IInternalCertificateXmlEncryptor.PerformEncryption(System.Security.Cryptography.Xml.EncryptedXml encryptedXml, System.Xml.XmlElement elementToEncrypt) { throw null; }
        internal CertificateXmlEncryptor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.DataProtection.XmlEncryption.IInternalCertificateXmlEncryptor encryptor) { }
    }
    internal partial interface IInternalCertificateXmlEncryptor
    {
        System.Security.Cryptography.Xml.EncryptedData PerformEncryption(System.Security.Cryptography.Xml.EncryptedXml encryptedXml, System.Xml.XmlElement elementToEncrypt);
    }
    internal partial interface IInternalEncryptedXmlDecryptor
    {
        void PerformPreDecryptionSetup(System.Security.Cryptography.Xml.EncryptedXml encryptedXml);
    }
    internal static partial class XmlEncryptionExtensions
    {
        public static System.Xml.Linq.XElement DecryptElement(this System.Xml.Linq.XElement element, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator) { throw null; }
        public static System.Xml.Linq.XElement EncryptIfNecessary(this Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor encryptor, System.Xml.Linq.XElement element) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.Secret ToSecret(this System.Xml.Linq.XElement element) { throw null; }
        public static System.Xml.Linq.XElement ToXElement(this Microsoft.AspNetCore.DataProtection.Secret secret) { throw null; }
    }
    internal partial class XmlKeyDecryptionOptions
    {
        public XmlKeyDecryptionOptions() { }
        public int KeyDecryptionCertificateCount { get { throw null; } }
        public void AddKeyDecryptionCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public bool TryGetKeyDecryptionCertificates(System.Security.Cryptography.X509Certificates.X509Certificate2 certInfo, out System.Collections.Generic.IReadOnlyList<System.Security.Cryptography.X509Certificates.X509Certificate2> keyDecryptionCerts) { throw null; }
    }
}