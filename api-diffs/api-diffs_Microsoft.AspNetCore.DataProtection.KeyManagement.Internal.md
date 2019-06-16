# Microsoft.AspNetCore.DataProtection.KeyManagement.Internal

``` diff
 namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal {
     public sealed class CacheableKeyRing
     public struct DefaultKeyResolution {
         public IKey DefaultKey;
         public IKey FallbackKey;
         public bool ShouldGenerateNewKey;
     }
     public interface ICacheableKeyRingProvider {
         CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now);
     }
     public interface IDefaultKeyResolver {
         DefaultKeyResolution ResolveDefaultKeyPolicy(DateTimeOffset now, IEnumerable<IKey> allKeys);
     }
     public interface IInternalXmlKeyManager {
         IKey CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate);
         IAuthenticatedEncryptorDescriptor DeserializeDescriptorFromKeyElement(XElement keyElement);
         void RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string reason);
     }
     public interface IKeyRing {
         IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; }
         Guid DefaultKeyId { get; }
         IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked);
     }
     public interface IKeyRingProvider {
         IKeyRing GetCurrentKeyRing();
     }
 }
```

