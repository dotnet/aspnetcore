using System;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal sealed unsafe class DataProtectionProviderImpl : IDataProtectionProvider
    {
        private readonly BCryptKeyHandle _kdfSubkeyHandle;

        public DataProtectionProviderImpl(BCryptKeyHandle kdfSubkeyHandle)
        {
            _kdfSubkeyHandle = kdfSubkeyHandle;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            BCryptKeyHandle newAesKeyHandle;
            BCryptHashHandle newHmacHashHandle;
            BCryptKeyHandle newKdfSubkeyHandle;

            BCryptUtil.DeriveKeysSP800108(Algorithms.SP800108AlgorithmHandle, _kdfSubkeyHandle, purpose, Algorithms.AESAlgorithmHandle, out newAesKeyHandle, Algorithms.HMACSHA256AlgorithmHandle, out newHmacHashHandle, out newKdfSubkeyHandle);
            return new DataProtectorImpl(newAesKeyHandle, newHmacHashHandle, newKdfSubkeyHandle);
        }

        public void Dispose()
        {
            _kdfSubkeyHandle.Dispose();
        }
    }
}