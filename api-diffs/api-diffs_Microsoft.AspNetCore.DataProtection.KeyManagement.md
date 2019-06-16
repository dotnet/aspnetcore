# Microsoft.AspNetCore.DataProtection.KeyManagement

``` diff
 namespace Microsoft.AspNetCore.DataProtection.KeyManagement {
     public interface IKey {
         DateTimeOffset ActivationDate { get; }
         DateTimeOffset CreationDate { get; }
         IAuthenticatedEncryptorDescriptor Descriptor { get; }
         DateTimeOffset ExpirationDate { get; }
         bool IsRevoked { get; }
         Guid KeyId { get; }
         IAuthenticatedEncryptor CreateEncryptor();
     }
     public interface IKeyEscrowSink {
         void Store(Guid keyId, XElement element);
     }
     public interface IKeyManager {
         IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate);
         IReadOnlyCollection<IKey> GetAllKeys();
         CancellationToken GetCacheExpirationToken();
         void RevokeAllKeys(DateTimeOffset revocationDate, string reason = null);
         void RevokeKey(Guid keyId, string reason = null);
     }
     public class KeyManagementOptions {
         public KeyManagementOptions();
         public AlgorithmConfiguration AuthenticatedEncryptorConfiguration { get; set; }
         public IList<IAuthenticatedEncryptorFactory> AuthenticatedEncryptorFactories { get; }
         public bool AutoGenerateKeys { get; set; }
         public IList<IKeyEscrowSink> KeyEscrowSinks { get; }
         public TimeSpan NewKeyLifetime { get; set; }
         public IXmlEncryptor XmlEncryptor { get; set; }
         public IXmlRepository XmlRepository { get; set; }
     }
     public sealed class XmlKeyManager : IInternalXmlKeyManager, IKeyManager {
         public XmlKeyManager(IOptions<KeyManagementOptions> keyManagementOptions, IActivator activator);
         public XmlKeyManager(IOptions<KeyManagementOptions> keyManagementOptions, IActivator activator, ILoggerFactory loggerFactory);
         public IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate);
         public IReadOnlyCollection<IKey> GetAllKeys();
         public CancellationToken GetCacheExpirationToken();
         IKey Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate);
         IAuthenticatedEncryptorDescriptor Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.DeserializeDescriptorFromKeyElement(XElement keyElement);
         void Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string reason);
         public void RevokeAllKeys(DateTimeOffset revocationDate, string reason = null);
         public void RevokeKey(Guid keyId, string reason = null);
     }
 }
```

