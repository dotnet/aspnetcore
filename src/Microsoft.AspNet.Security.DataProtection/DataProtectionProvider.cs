using System;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection
{
    public static unsafe class DataProtectionProvider
    {
        private const int MASTER_KEY_REQUIRED_LENGTH = 512/8;

        private static readonly byte[] MASTER_SUBKEY_GENERATOR = GetMasterSubkeyGenerator();
        private static readonly byte[] MASTER_DPAPI_ENTROPY = GetMasterSubkeyGenerator(isDpapi: true);

        private static byte[] GetMasterSubkeyGenerator(bool isDpapi = false)
        {
            TypeInfo typeInfo = ((isDpapi) ? typeof(DpapiDataProtectionProviderImpl) : typeof(DataProtectionProvider)).GetTypeInfo();

            byte[] retVal = new byte[sizeof (Guid)*2];
            fixed (byte* pRetVal = retVal)
            {
                Guid* guids = (Guid*) pRetVal;
                guids[0] = typeInfo.GUID;
#if NET45
                guids[1] = typeInfo.Module.ModuleVersionId;
#else
                guids[1] = default(Guid);
#endif
            }
            return retVal;
        }

        /// <summary>
        /// Creates a new IDataProtectionProvider backed by DPAPI.
        /// </summary>
        public static IDataProtectionProvider CreateFromDpapi()
        {
            return new DpapiDataProtectionProviderImpl(MASTER_DPAPI_ENTROPY);
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
                BufferUtil.ZeroMemory(masterKey, MASTER_KEY_REQUIRED_LENGTH);
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
                using (var hashHandle = BCryptUtil.CreateHash(Algorithms.HMACSHA512AlgorithmHandle, masterKey, masterKeyLengthInBytes))
                {
                    BCryptUtil.HashData(hashHandle, masterKey, masterKeyLengthInBytes, masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
                }
                BCryptKeyHandle kdfSubkeyHandle = BCryptUtil.ImportKey(Algorithms.SP800108AlgorithmHandle, masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
                return new DataProtectionProviderImpl(kdfSubkeyHandle);
            }
            finally
            {
                BufferUtil.ZeroMemory(masterSubkey, MASTER_KEY_REQUIRED_LENGTH);
            }
        }
    }
}