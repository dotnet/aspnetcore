// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class DataProtectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddKeyEscrowSink(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink sink) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddKeyEscrowSink(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.Func<System.IServiceProvider, Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink> factory) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddKeyEscrowSink<TImplementation>(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) where TImplementation : class, Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddKeyManagementOptions(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.Action<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> setupAction) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder DisableAutomaticKeyGeneration(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToFileSystem(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.IO.DirectoryInfo directory) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToRegistry(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.Win32.RegistryKey registryKey) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithCertificate(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithCertificate(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, string thumbprint) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithDpapi(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithDpapi(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, bool protectToLocalMachine) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithDpapiNG(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithDpapiNG(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, string protectionDescriptorRule, Microsoft.AspNetCore.DataProtection.XmlEncryption.DpapiNGProtectionDescriptorFlags flags) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder SetApplicationName(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, string applicationName) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder SetDefaultKeyLifetime(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.TimeSpan lifetime) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UnprotectKeysWithAnyCertificate(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, params System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UseCryptographicAlgorithms(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorConfiguration configuration) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UseCustomCryptographicAlgorithms(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngCbcAuthenticatedEncryptorConfiguration configuration) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UseCustomCryptographicAlgorithms(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngGcmAuthenticatedEncryptorConfiguration configuration) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UseCustomCryptographicAlgorithms(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.ManagedAuthenticatedEncryptorConfiguration configuration) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder UseEphemeralDataProtectionProvider(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder) { throw null; }
    }
    public partial class DataProtectionOptions
    {
        public DataProtectionOptions() { }
        public string ApplicationDiscriminator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class DataProtectionUtilityExtensions
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static string GetApplicationUniqueIdentifier(this System.IServiceProvider services) { throw null; }
    }
    public sealed partial class EphemeralDataProtectionProvider : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider
    {
        public EphemeralDataProtectionProvider() { }
        public EphemeralDataProtectionProvider(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) { throw null; }
    }
    public partial interface IDataProtectionBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public partial interface IPersistedDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider, Microsoft.AspNetCore.DataProtection.IDataProtector
    {
        byte[] DangerousUnprotect(byte[] protectedData, bool ignoreRevocationErrors, out bool requiresMigration, out bool wasRevoked);
    }
    public partial interface ISecret : System.IDisposable
    {
        int Length { get; }
        void WriteSecretIntoBuffer(System.ArraySegment<byte> buffer);
    }
    public sealed partial class Secret : Microsoft.AspNetCore.DataProtection.ISecret, System.IDisposable
    {
        public Secret(Microsoft.AspNetCore.DataProtection.ISecret secret) { }
        public Secret(System.ArraySegment<byte> value) { }
        public unsafe Secret(byte* secret, int secretLength) { }
        public Secret(byte[] value) { }
        public int Length { get { throw null; } }
        public void Dispose() { }
        public static Microsoft.AspNetCore.DataProtection.Secret Random(int numBytes) { throw null; }
        public void WriteSecretIntoBuffer(System.ArraySegment<byte> buffer) { }
        public unsafe void WriteSecretIntoBuffer(byte* buffer, int bufferLength) { }
    }
}
namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    public sealed partial class AuthenticatedEncryptorFactory : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory
    {
        public AuthenticatedEncryptorFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptorInstance(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey key) { throw null; }
    }
    public sealed partial class CngCbcAuthenticatedEncryptorFactory : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory
    {
        public CngCbcAuthenticatedEncryptorFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptorInstance(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey key) { throw null; }
    }
    public sealed partial class CngGcmAuthenticatedEncryptorFactory : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory
    {
        public CngGcmAuthenticatedEncryptorFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptorInstance(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey key) { throw null; }
    }
    public enum EncryptionAlgorithm
    {
        AES_128_CBC = 0,
        AES_192_CBC = 1,
        AES_256_CBC = 2,
        AES_128_GCM = 3,
        AES_192_GCM = 4,
        AES_256_GCM = 5,
    }
    public partial interface IAuthenticatedEncryptor
    {
        byte[] Decrypt(System.ArraySegment<byte> ciphertext, System.ArraySegment<byte> additionalAuthenticatedData);
        byte[] Encrypt(System.ArraySegment<byte> plaintext, System.ArraySegment<byte> additionalAuthenticatedData);
    }
    public partial interface IAuthenticatedEncryptorFactory
    {
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptorInstance(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey key);
    }
    public sealed partial class ManagedAuthenticatedEncryptorFactory : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory
    {
        public ManagedAuthenticatedEncryptorFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptorInstance(Microsoft.AspNetCore.DataProtection.KeyManagement.IKey key) { throw null; }
    }
    public enum ValidationAlgorithm
    {
        HMACSHA256 = 0,
        HMACSHA512 = 1,
    }
}
namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public abstract partial class AlgorithmConfiguration
    {
        protected AlgorithmConfiguration() { }
        public abstract Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
    }
    public sealed partial class AuthenticatedEncryptorConfiguration : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration
    {
        public AuthenticatedEncryptorConfiguration() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.EncryptionAlgorithm EncryptionAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ValidationAlgorithm ValidationAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor CreateNewDescriptor() { throw null; }
    }
    public sealed partial class AuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        public AuthenticatedEncryptorDescriptor(Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorConfiguration configuration, Microsoft.AspNetCore.DataProtection.ISecret masterKey) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.XmlSerializedDescriptorInfo ExportToXml() { throw null; }
    }
    public sealed partial class AuthenticatedEncryptorDescriptorDeserializer : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptorDeserializer
    {
        public AuthenticatedEncryptorDescriptorDeserializer() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor ImportFromXml(System.Xml.Linq.XElement element) { throw null; }
    }
    public sealed partial class CngCbcAuthenticatedEncryptorConfiguration : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration
    {
        public CngCbcAuthenticatedEncryptorConfiguration() { }
        public string EncryptionAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int EncryptionAlgorithmKeySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EncryptionAlgorithmProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string HashAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string HashAlgorithmProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor CreateNewDescriptor() { throw null; }
    }
    public sealed partial class CngCbcAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        public CngCbcAuthenticatedEncryptorDescriptor(Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngCbcAuthenticatedEncryptorConfiguration configuration, Microsoft.AspNetCore.DataProtection.ISecret masterKey) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.XmlSerializedDescriptorInfo ExportToXml() { throw null; }
    }
    public sealed partial class CngCbcAuthenticatedEncryptorDescriptorDeserializer : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptorDeserializer
    {
        public CngCbcAuthenticatedEncryptorDescriptorDeserializer() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor ImportFromXml(System.Xml.Linq.XElement element) { throw null; }
    }
    public sealed partial class CngGcmAuthenticatedEncryptorConfiguration : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration
    {
        public CngGcmAuthenticatedEncryptorConfiguration() { }
        public string EncryptionAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int EncryptionAlgorithmKeySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EncryptionAlgorithmProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor CreateNewDescriptor() { throw null; }
    }
    public sealed partial class CngGcmAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        public CngGcmAuthenticatedEncryptorDescriptor(Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.CngGcmAuthenticatedEncryptorConfiguration configuration, Microsoft.AspNetCore.DataProtection.ISecret masterKey) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.XmlSerializedDescriptorInfo ExportToXml() { throw null; }
    }
    public sealed partial class CngGcmAuthenticatedEncryptorDescriptorDeserializer : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptorDeserializer
    {
        public CngGcmAuthenticatedEncryptorDescriptorDeserializer() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor ImportFromXml(System.Xml.Linq.XElement element) { throw null; }
    }
    public partial interface IAuthenticatedEncryptorDescriptor
    {
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.XmlSerializedDescriptorInfo ExportToXml();
    }
    public partial interface IAuthenticatedEncryptorDescriptorDeserializer
    {
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor ImportFromXml(System.Xml.Linq.XElement element);
    }
    public sealed partial class ManagedAuthenticatedEncryptorConfiguration : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration
    {
        public ManagedAuthenticatedEncryptorConfiguration() { }
        public int EncryptionAlgorithmKeySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type EncryptionAlgorithmType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type ValidationAlgorithmType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor CreateNewDescriptor() { throw null; }
    }
    public sealed partial class ManagedAuthenticatedEncryptorDescriptor : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor
    {
        public ManagedAuthenticatedEncryptorDescriptor(Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.ManagedAuthenticatedEncryptorConfiguration configuration, Microsoft.AspNetCore.DataProtection.ISecret masterKey) { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.XmlSerializedDescriptorInfo ExportToXml() { throw null; }
    }
    public sealed partial class ManagedAuthenticatedEncryptorDescriptorDeserializer : Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptorDeserializer
    {
        public ManagedAuthenticatedEncryptorDescriptorDeserializer() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor ImportFromXml(System.Xml.Linq.XElement element) { throw null; }
    }
    public static partial class XmlExtensions
    {
        public static void MarkAsRequiresEncryption(this System.Xml.Linq.XElement element) { }
    }
    public sealed partial class XmlSerializedDescriptorInfo
    {
        public XmlSerializedDescriptorInfo(System.Xml.Linq.XElement serializedDescriptorElement, System.Type deserializerType) { }
        public System.Type DeserializerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.Linq.XElement SerializedDescriptorElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.DataProtection.Internal
{
    public partial interface IActivator
    {
        object CreateInstance(System.Type expectedBaseType, string implementationTypeName);
    }
}
namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public partial interface IKey
    {
        System.DateTimeOffset ActivationDate { get; }
        System.DateTimeOffset CreationDate { get; }
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor Descriptor { get; }
        System.DateTimeOffset ExpirationDate { get; }
        bool IsRevoked { get; }
        System.Guid KeyId { get; }
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor CreateEncryptor();
    }
    public partial interface IKeyEscrowSink
    {
        void Store(System.Guid keyId, System.Xml.Linq.XElement element);
    }
    public partial interface IKeyManager
    {
        Microsoft.AspNetCore.DataProtection.KeyManagement.IKey CreateNewKey(System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate);
        System.Collections.Generic.IReadOnlyCollection<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> GetAllKeys();
        System.Threading.CancellationToken GetCacheExpirationToken();
        void RevokeAllKeys(System.DateTimeOffset revocationDate, string reason = null);
        void RevokeKey(System.Guid keyId, string reason = null);
    }
    public partial class KeyManagementOptions
    {
        public KeyManagementOptions() { }
        public Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AlgorithmConfiguration AuthenticatedEncryptorConfiguration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptorFactory> AuthenticatedEncryptorFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool AutoGenerateKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyEscrowSink> KeyEscrowSinks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.TimeSpan NewKeyLifetime { get { throw null; } set { } }
        public Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor XmlEncryptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository XmlRepository { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public sealed partial class XmlKeyManager : Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager, Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager
    {
        public XmlKeyManager(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator) { }
        public XmlKeyManager(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions> keyManagementOptions, Microsoft.AspNetCore.DataProtection.Internal.IActivator activator, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.KeyManagement.IKey CreateNewKey(System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate) { throw null; }
        public System.Collections.Generic.IReadOnlyCollection<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> GetAllKeys() { throw null; }
        public System.Threading.CancellationToken GetCacheExpirationToken() { throw null; }
        Microsoft.AspNetCore.DataProtection.KeyManagement.IKey Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.CreateNewKey(System.Guid keyId, System.DateTimeOffset creationDate, System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate) { throw null; }
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.DeserializeDescriptorFromKeyElement(System.Xml.Linq.XElement keyElement) { throw null; }
        void Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.RevokeSingleKey(System.Guid keyId, System.DateTimeOffset revocationDate, string reason) { }
        public void RevokeAllKeys(System.DateTimeOffset revocationDate, string reason = null) { }
        public void RevokeKey(System.Guid keyId, string reason = null) { }
    }
}
namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal
{
    public sealed partial class CacheableKeyRing
    {
        internal CacheableKeyRing() { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct DefaultKeyResolution
    {
        public Microsoft.AspNetCore.DataProtection.KeyManagement.IKey DefaultKey;
        public Microsoft.AspNetCore.DataProtection.KeyManagement.IKey FallbackKey;
        public bool ShouldGenerateNewKey;
    }
    public partial interface ICacheableKeyRingProvider
    {
        Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.CacheableKeyRing GetCacheableKeyRing(System.DateTimeOffset now);
    }
    public partial interface IDefaultKeyResolver
    {
        Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.DefaultKeyResolution ResolveDefaultKeyPolicy(System.DateTimeOffset now, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.DataProtection.KeyManagement.IKey> allKeys);
    }
    public partial interface IInternalXmlKeyManager
    {
        Microsoft.AspNetCore.DataProtection.KeyManagement.IKey CreateNewKey(System.Guid keyId, System.DateTimeOffset creationDate, System.DateTimeOffset activationDate, System.DateTimeOffset expirationDate);
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.IAuthenticatedEncryptorDescriptor DeserializeDescriptorFromKeyElement(System.Xml.Linq.XElement keyElement);
        void RevokeSingleKey(System.Guid keyId, System.DateTimeOffset revocationDate, string reason);
    }
    public partial interface IKeyRing
    {
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; }
        System.Guid DefaultKeyId { get; }
        Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(System.Guid keyId, out bool isRevoked);
    }
    public partial interface IKeyRingProvider
    {
        Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IKeyRing GetCurrentKeyRing();
    }
}
namespace Microsoft.AspNetCore.DataProtection.Repositories
{
    public partial class FileSystemXmlRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        public FileSystemXmlRepository(System.IO.DirectoryInfo directory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public static System.IO.DirectoryInfo DefaultKeyStorageDirectory { get { throw null; } }
        public System.IO.DirectoryInfo Directory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public virtual void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
    public partial interface IXmlRepository
    {
        System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements();
        void StoreElement(System.Xml.Linq.XElement element, string friendlyName);
    }
    public partial class RegistryXmlRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        public RegistryXmlRepository(Microsoft.Win32.RegistryKey registryKey, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public static Microsoft.Win32.RegistryKey DefaultRegistryKey { get { throw null; } }
        public Microsoft.Win32.RegistryKey RegistryKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public virtual void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
}
namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    public partial class CertificateResolver : Microsoft.AspNetCore.DataProtection.XmlEncryption.ICertificateResolver
    {
        public CertificateResolver() { }
        public virtual System.Security.Cryptography.X509Certificates.X509Certificate2 ResolveCertificate(string thumbprint) { throw null; }
    }
    public sealed partial class CertificateXmlEncryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor
    {
        public CertificateXmlEncryptor(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public CertificateXmlEncryptor(string thumbprint, Microsoft.AspNetCore.DataProtection.XmlEncryption.ICertificateResolver certificateResolver, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlInfo Encrypt(System.Xml.Linq.XElement plaintextElement) { throw null; }
    }
    [System.FlagsAttribute]
    public enum DpapiNGProtectionDescriptorFlags
    {
        None = 0,
        NamedDescriptor = 1,
        MachineKey = 32,
    }
    public sealed partial class DpapiNGXmlDecryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlDecryptor
    {
        public DpapiNGXmlDecryptor() { }
        public DpapiNGXmlDecryptor(System.IServiceProvider services) { }
        public System.Xml.Linq.XElement Decrypt(System.Xml.Linq.XElement encryptedElement) { throw null; }
    }
    public sealed partial class DpapiNGXmlEncryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor
    {
        public DpapiNGXmlEncryptor(string protectionDescriptorRule, Microsoft.AspNetCore.DataProtection.XmlEncryption.DpapiNGProtectionDescriptorFlags flags, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlInfo Encrypt(System.Xml.Linq.XElement plaintextElement) { throw null; }
    }
    public sealed partial class DpapiXmlDecryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlDecryptor
    {
        public DpapiXmlDecryptor() { }
        public DpapiXmlDecryptor(System.IServiceProvider services) { }
        public System.Xml.Linq.XElement Decrypt(System.Xml.Linq.XElement encryptedElement) { throw null; }
    }
    public sealed partial class DpapiXmlEncryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor
    {
        public DpapiXmlEncryptor(bool protectToLocalMachine, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlInfo Encrypt(System.Xml.Linq.XElement plaintextElement) { throw null; }
    }
    public sealed partial class EncryptedXmlDecryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlDecryptor
    {
        public EncryptedXmlDecryptor() { }
        public EncryptedXmlDecryptor(System.IServiceProvider services) { }
        public System.Xml.Linq.XElement Decrypt(System.Xml.Linq.XElement encryptedElement) { throw null; }
    }
    public sealed partial class EncryptedXmlInfo
    {
        public EncryptedXmlInfo(System.Xml.Linq.XElement encryptedElement, System.Type decryptorType) { }
        public System.Type DecryptorType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.Linq.XElement EncryptedElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface ICertificateResolver
    {
        System.Security.Cryptography.X509Certificates.X509Certificate2 ResolveCertificate(string thumbprint);
    }
    public partial interface IXmlDecryptor
    {
        System.Xml.Linq.XElement Decrypt(System.Xml.Linq.XElement encryptedElement);
    }
    public partial interface IXmlEncryptor
    {
        Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlInfo Encrypt(System.Xml.Linq.XElement plaintextElement);
    }
    public sealed partial class NullXmlDecryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlDecryptor
    {
        public NullXmlDecryptor() { }
        public System.Xml.Linq.XElement Decrypt(System.Xml.Linq.XElement encryptedElement) { throw null; }
    }
    public sealed partial class NullXmlEncryptor : Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor
    {
        public NullXmlEncryptor() { }
        public NullXmlEncryptor(System.IServiceProvider services) { }
        public Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlInfo Encrypt(System.Xml.Linq.XElement plaintextElement) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DataProtectionServiceCollectionExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddDataProtection(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder AddDataProtection(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.DataProtection.DataProtectionOptions> setupAction) { throw null; }
    }
}
