using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Provides methods for creating IDataProtectionProvider instances.
    /// </summary>
    public unsafe static class DataProtectionProvider
    {
        const int MASTER_KEY_REQUIRED_LENGTH = 512 / 8;

        private static readonly byte[] MASTER_SUBKEY_GENERATOR = Encoding.ASCII.GetBytes("Microsoft.AspNet.Security.DataProtection");

        /// <summary>
        /// Creates a new IDataProtectionProvider backed by DPAPI, where the protected
        /// payload can only be decrypted by the current user.
        /// </summary>
        public static IDataProtectionProvider CreateFromDpapi()
        {
            return CreateFromDpapi(protectToLocalMachine: false);
        }

        /// <summary>
        /// Creates a new IDataProtectionProvider backed by DPAPI.
        /// </summary>
        /// <param name="protectToLocalMachine">True if protected payloads can be decrypted by any user
        /// on the local machine, false if protected payloads should only be able to decrypted by the
        /// current user account.</param>
        public static IDataProtectionProvider CreateFromDpapi(bool protectToLocalMachine)
        {
            return new DpapiDataProtectionProviderImpl(MASTER_SUBKEY_GENERATOR, protectToLocalMachine);
        }

        /// <summary>
        /// Creates a new IDataProtectionProvider with a randomly-generated master key.
        /// </summary>
        public static IDataProtectionProvider CreateNew()
        {
            byte* masterKey = stackalloc byte[MASTER_KEY_REQUIRED_LENGTH];
            try
            {
                BCryptUtil.GenRandom(masterKey, MASTER_KEY_REQUIRED_LENGTH);
                return CreateImpl(masterKey, MASTER_KEY_REQUIRED_LENGTH);
            }
            finally
            {
                BufferUtil.SecureZeroMemory(masterKey, MASTER_KEY_REQUIRED_LENGTH);
            }
        }

        /// <summary>
        /// Creates a new IDataProtectionProvider with the provided master key.
        /// </summary>
        public static IDataProtectionProvider CreateFromKey(byte[] masterKey)
        {
            if (masterKey == null)
            {
                throw new ArgumentNullException("masterKey");
            }
            if (masterKey.Length < MASTER_KEY_REQUIRED_LENGTH)
            {
                string errorMessage = String.Format(CultureInfo.CurrentCulture, Res.DataProtectorFactory_MasterKeyTooShort, MASTER_KEY_REQUIRED_LENGTH);
                throw new ArgumentOutOfRangeException("masterKey", errorMessage);
            }

            fixed (byte* pMasterKey = masterKey)
            {
                return CreateImpl(pMasterKey, masterKey.Length);
            }
        }

        private static DataProtectionProviderImpl CreateImpl(byte* masterKey, int masterKeyLengthInBytes)
        {
            // We don't use the master key directly. We derive a master subkey via HMAC_{master_key}(MASTER_SUBKEY_GENERATOR).
            byte* masterSubkey = stackalloc byte[MASTER_KEY_REQUIRED_LENGTH];
            try
            {
                using (var hashHandle = BCryptUtil.CreateHMACHandle(Algorithms.HMACSHA512AlgorithmHandle, masterKey, masterKeyLengthInBytes))
                {
                    fixed (byte* pMasterSubkeyGenerator = MASTER_SUBKEY_GENERATOR)
                    {
                        BCryptUtil.HashData(hashHandle, pMasterSubkeyGenerator, MASTER_SUBKEY_GENERATOR.Length, masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
                    }
                }
                byte[] protectedKdk = BufferUtil.ToProtectedManagedByteArray(masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
                return new DataProtectionProviderImpl(protectedKdk);
            }
            finally
            {
                BufferUtil.SecureZeroMemory(masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
            }
        }
    }
}
