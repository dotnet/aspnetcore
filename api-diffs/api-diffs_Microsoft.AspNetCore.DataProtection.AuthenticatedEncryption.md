# Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption

``` diff
 namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption {
     public sealed class AuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory {
         public AuthenticatedEncryptorFactory(ILoggerFactory loggerFactory);
         public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key);
     }
     public sealed class CngCbcAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory {
         public CngCbcAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory);
         public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key);
     }
     public sealed class CngGcmAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory {
         public CngGcmAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory);
         public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key);
     }
     public enum EncryptionAlgorithm {
         AES_128_CBC = 0,
         AES_128_GCM = 3,
         AES_192_CBC = 1,
         AES_192_GCM = 4,
         AES_256_CBC = 2,
         AES_256_GCM = 5,
     }
     public interface IAuthenticatedEncryptor {
         byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData);
         byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData);
     }
     public interface IAuthenticatedEncryptorFactory {
         IAuthenticatedEncryptor CreateEncryptorInstance(IKey key);
     }
     public sealed class ManagedAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory {
         public ManagedAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory);
         public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key);
     }
     public enum ValidationAlgorithm {
         HMACSHA256 = 0,
         HMACSHA512 = 1,
     }
 }
```

