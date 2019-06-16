# Microsoft.AspNetCore.DataProtection.Cng.Internal

``` diff
 namespace Microsoft.AspNetCore.DataProtection.Cng.Internal {
     public abstract class CngAuthenticatedEncryptorBase : IAuthenticatedEncryptor, IDisposable, IOptimizedAuthenticatedEncryptor {
         protected CngAuthenticatedEncryptorBase();
         public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData);
         protected unsafe abstract byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData);
         public abstract void Dispose();
         public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData);
         public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize);
         protected unsafe abstract byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer);
     }
 }
```

