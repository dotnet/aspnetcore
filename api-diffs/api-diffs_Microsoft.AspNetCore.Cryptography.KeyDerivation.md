# Microsoft.AspNetCore.Cryptography.KeyDerivation

``` diff
 namespace Microsoft.AspNetCore.Cryptography.KeyDerivation {
     public static class KeyDerivation {
         public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested);
     }
     public enum KeyDerivationPrf {
         HMACSHA1 = 0,
         HMACSHA256 = 1,
         HMACSHA512 = 2,
     }
 }
```

