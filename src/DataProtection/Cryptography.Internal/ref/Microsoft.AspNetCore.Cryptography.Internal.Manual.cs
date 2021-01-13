// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cryptography
{
    internal static partial class Constants
    {
        internal const string BCRYPT_3DES_112_ALGORITHM = "3DES_112";
        internal const string BCRYPT_3DES_ALGORITHM = "3DES";
        internal const string BCRYPT_AES_ALGORITHM = "AES";
        internal const string BCRYPT_AES_CMAC_ALGORITHM = "AES-CMAC";
        internal const string BCRYPT_AES_GMAC_ALGORITHM = "AES-GMAC";
        internal const string BCRYPT_AES_WRAP_KEY_BLOB = "Rfc3565KeyWrapBlob";
        internal const string BCRYPT_ALGORITHM_NAME = "AlgorithmName";
        internal const string BCRYPT_AUTH_TAG_LENGTH = "AuthTagLength";
        internal const string BCRYPT_BLOCK_LENGTH = "BlockLength";
        internal const string BCRYPT_BLOCK_SIZE_LIST = "BlockSizeList";
        internal const string BCRYPT_CAPI_KDF_ALGORITHM = "CAPI_KDF";
        internal const string BCRYPT_CHAINING_MODE = "ChainingMode";
        internal const string BCRYPT_CHAIN_MODE_CBC = "ChainingModeCBC";
        internal const string BCRYPT_CHAIN_MODE_CCM = "ChainingModeCCM";
        internal const string BCRYPT_CHAIN_MODE_CFB = "ChainingModeCFB";
        internal const string BCRYPT_CHAIN_MODE_ECB = "ChainingModeECB";
        internal const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        internal const string BCRYPT_CHAIN_MODE_NA = "ChainingModeN/A";
        internal const string BCRYPT_DESX_ALGORITHM = "DESX";
        internal const string BCRYPT_DES_ALGORITHM = "DES";
        internal const string BCRYPT_DH_ALGORITHM = "DH";
        internal const string BCRYPT_DSA_ALGORITHM = "DSA";
        internal const string BCRYPT_ECDH_P256_ALGORITHM = "ECDH_P256";
        internal const string BCRYPT_ECDH_P384_ALGORITHM = "ECDH_P384";
        internal const string BCRYPT_ECDH_P521_ALGORITHM = "ECDH_P521";
        internal const string BCRYPT_ECDSA_P256_ALGORITHM = "ECDSA_P256";
        internal const string BCRYPT_ECDSA_P384_ALGORITHM = "ECDSA_P384";
        internal const string BCRYPT_ECDSA_P521_ALGORITHM = "ECDSA_P521";
        internal const string BCRYPT_EFFECTIVE_KEY_LENGTH = "EffectiveKeyLength";
        internal const string BCRYPT_HASH_BLOCK_LENGTH = "HashBlockLength";
        internal const string BCRYPT_HASH_LENGTH = "HashDigestLength";
        internal const string BCRYPT_HASH_OID_LIST = "HashOIDList";
        internal const string BCRYPT_IS_KEYED_HASH = "IsKeyedHash";
        internal const string BCRYPT_IS_REUSABLE_HASH = "IsReusableHash";
        internal const string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";
        internal const string BCRYPT_KEY_LENGTH = "KeyLength";
        internal const string BCRYPT_KEY_LENGTHS = "KeyLengths";
        internal const string BCRYPT_KEY_OBJECT_LENGTH = "KeyObjectLength";
        internal const string BCRYPT_KEY_STRENGTH = "KeyStrength";
        internal const string BCRYPT_MD2_ALGORITHM = "MD2";
        internal const string BCRYPT_MD4_ALGORITHM = "MD4";
        internal const string BCRYPT_MD5_ALGORITHM = "MD5";
        internal const string BCRYPT_MESSAGE_BLOCK_LENGTH = "MessageBlockLength";
        internal const string BCRYPT_OBJECT_LENGTH = "ObjectLength";
        internal const string BCRYPT_OPAQUE_KEY_BLOB = "OpaqueKeyBlob";
        internal const string BCRYPT_PADDING_SCHEMES = "PaddingSchemes";
        internal const string BCRYPT_PBKDF2_ALGORITHM = "PBKDF2";
        internal const string BCRYPT_PRIMITIVE_TYPE = "PrimitiveType";
        internal const string BCRYPT_PROVIDER_HANDLE = "ProviderHandle";
        internal const string BCRYPT_RC2_ALGORITHM = "RC2";
        internal const string BCRYPT_RC4_ALGORITHM = "RC4";
        internal const string BCRYPT_RNG_ALGORITHM = "RNG";
        internal const string BCRYPT_RNG_DUAL_EC_ALGORITHM = "DUALECRNG";
        internal const string BCRYPT_RNG_FIPS186_DSA_ALGORITHM = "FIPS186DSARNG";
        internal const string BCRYPT_RSA_ALGORITHM = "RSA";
        internal const string BCRYPT_RSA_SIGN_ALGORITHM = "RSA_SIGN";
        internal const string BCRYPT_SHA1_ALGORITHM = "SHA1";
        internal const string BCRYPT_SHA256_ALGORITHM = "SHA256";
        internal const string BCRYPT_SHA384_ALGORITHM = "SHA384";
        internal const string BCRYPT_SHA512_ALGORITHM = "SHA512";
        internal const string BCRYPT_SIGNATURE_LENGTH = "SignatureLength";
        internal const string BCRYPT_SP800108_CTR_HMAC_ALGORITHM = "SP800_108_CTR_HMAC";
        internal const string BCRYPT_SP80056A_CONCAT_ALGORITHM = "SP800_56A_CONCAT";
        internal const int MAX_STACKALLOC_BYTES = 256;
        internal const string MS_PLATFORM_CRYPTO_PROVIDER = "Microsoft Platform Crypto Provider";
        internal const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";
    }
    internal static partial class CryptoUtil
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void Assert(bool condition, string message) { }
        public static void AssertPlatformIsWindows() { }
        public static void AssertPlatformIsWindows8OrLater() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void AssertSafeHandleIsValid(System.Runtime.InteropServices.SafeHandle safeHandle) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Exception Fail(string message) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static T Fail<T>(string message) where T : class { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static bool TimeConstantBuffersAreEqual(byte* bufA, byte* bufB, uint count) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static bool TimeConstantBuffersAreEqual(byte[] bufA, int offsetA, int countA, byte[] bufB, int offsetB, int countB) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe partial struct DATA_BLOB
    {
        public uint cbData;
        public byte* pbData;
    }
    internal static partial class UnsafeBufferUtil
    {
        [System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.MayFail)]
        public static void BlockCopy(Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle from, Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle to, System.IntPtr length) { }
        [System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.MayFail)]
        public unsafe static void BlockCopy(Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle from, void* to, uint byteCount) { }
        [System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.MayFail)]
        public unsafe static void BlockCopy(void* from, Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle to, uint byteCount) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void BlockCopy(void* from, void* to, int byteCount) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void BlockCopy(void* from, void* to, uint byteCount) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void SecureZeroMemory(byte* buffer, int byteCount) { }
        [System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void SecureZeroMemory(byte* buffer, System.IntPtr length) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void SecureZeroMemory(byte* buffer, uint byteCount) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        public unsafe static void SecureZeroMemory(byte* buffer, ulong byteCount) { }
    }
    [System.Security.SuppressUnmanagedCodeSecurityAttribute]
    internal static partial class UnsafeNativeMethods
    {
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal static extern int BCryptCloseAlgorithmProvider(System.IntPtr hAlgorithm, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptCreateHash(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle hAlgorithm, out Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle phHash, System.IntPtr pbHashObject, uint cbHashObject, byte* pbSecret, uint cbSecret, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptDecrypt(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptKeyHandle hKey, byte* pbInput, uint cbInput, void* pPaddingInfo, byte* pbIV, uint cbIV, byte* pbOutput, uint cbOutput, out uint pcbResult, Microsoft.AspNetCore.Cryptography.Cng.BCryptEncryptFlags dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptDeriveKeyPBKDF2(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle hPrf, byte* pbPassword, uint cbPassword, byte* pbSalt, uint cbSalt, ulong cIterations, byte* pbDerivedKey, uint cbDerivedKey, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        internal static extern int BCryptDestroyHash(System.IntPtr hHash);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        internal static extern int BCryptDestroyKey(System.IntPtr hKey);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal static extern int BCryptDuplicateHash(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle hHash, out Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle phNewHash, System.IntPtr pbHashObject, uint cbHashObject, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptEncrypt(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptKeyHandle hKey, byte* pbInput, uint cbInput, void* pPaddingInfo, byte* pbIV, uint cbIV, byte* pbOutput, uint cbOutput, out uint pcbResult, Microsoft.AspNetCore.Cryptography.Cng.BCryptEncryptFlags dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptFinishHash(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle hHash, byte* pbOutput, uint cbOutput, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptGenerateSymmetricKey(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle hAlgorithm, out Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptKeyHandle phKey, System.IntPtr pbKeyObject, uint cbKeyObject, byte* pbSecret, uint cbSecret, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptGenRandom(System.IntPtr hAlgorithm, byte* pbBuffer, uint cbBuffer, Microsoft.AspNetCore.Cryptography.Cng.BCryptGenRandomFlags dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptGetProperty(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHandle hObject, string pszProperty, void* pbOutput, uint cbOutput, out uint pcbResult, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptHashData(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle hHash, byte* pbInput, uint cbInput, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptKeyDerivation(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptKeyHandle hKey, Microsoft.AspNetCore.Cryptography.Cng.BCryptBufferDesc* pParameterList, byte* pbDerivedKey, uint cbDerivedKey, out uint pcbResult, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal static extern int BCryptOpenAlgorithmProvider(out Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle phAlgorithm, string pszAlgId, string pszImplementation, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("bcrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int BCryptSetProperty(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHandle hObject, string pszProperty, void* pbInput, uint cbInput, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("crypt32.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern bool CryptProtectData(Microsoft.AspNetCore.Cryptography.DATA_BLOB* pDataIn, System.IntPtr szDataDescr, Microsoft.AspNetCore.Cryptography.DATA_BLOB* pOptionalEntropy, System.IntPtr pvReserved, System.IntPtr pPromptStruct, uint dwFlags, out Microsoft.AspNetCore.Cryptography.DATA_BLOB pDataOut);
        [System.Runtime.InteropServices.DllImport("crypt32.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]public static extern bool CryptProtectMemory(System.Runtime.InteropServices.SafeHandle pData, uint cbData, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("crypt32.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern bool CryptUnprotectData(Microsoft.AspNetCore.Cryptography.DATA_BLOB* pDataIn, System.IntPtr ppszDataDescr, Microsoft.AspNetCore.Cryptography.DATA_BLOB* pOptionalEntropy, System.IntPtr pvReserved, System.IntPtr pPromptStruct, uint dwFlags, out Microsoft.AspNetCore.Cryptography.DATA_BLOB pDataOut);
        [System.Runtime.InteropServices.DllImport("crypt32.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]public unsafe static extern bool CryptUnprotectMemory(byte* pData, uint cbData, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("crypt32.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]public static extern bool CryptUnprotectMemory(System.Runtime.InteropServices.SafeHandle pData, uint cbData, uint dwFlags);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)][System.Runtime.ConstrainedExecution.ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success)]
        internal static extern int NCryptCloseProtectionDescriptor(System.IntPtr hDescriptor);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal static extern int NCryptCreateProtectionDescriptor(string pwszDescriptorString, uint dwFlags, out Microsoft.AspNetCore.Cryptography.SafeHandles.NCryptDescriptorHandle phDescriptor);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal static extern int NCryptGetProtectionDescriptorInfo(Microsoft.AspNetCore.Cryptography.SafeHandles.NCryptDescriptorHandle hDescriptor, System.IntPtr pMemPara, uint dwInfoType, out Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle ppvInfo);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int NCryptProtectSecret(Microsoft.AspNetCore.Cryptography.SafeHandles.NCryptDescriptorHandle hDescriptor, uint dwFlags, byte* pbData, uint cbData, System.IntPtr pMemPara, System.IntPtr hWnd, out Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle ppbProtectedBlob, out uint pcbProtectedBlob);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int NCryptUnprotectSecret(out Microsoft.AspNetCore.Cryptography.SafeHandles.NCryptDescriptorHandle phDescriptor, uint dwFlags, byte* pbProtectedBlob, uint cbProtectedBlob, System.IntPtr pMemPara, System.IntPtr hWnd, out Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle ppbData, out uint pcbData);
        [System.Runtime.InteropServices.DllImport("ncrypt.dll")][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]internal unsafe static extern int NCryptUnprotectSecret(System.IntPtr phDescriptor, uint dwFlags, byte* pbProtectedBlob, uint cbProtectedBlob, System.IntPtr pMemPara, System.IntPtr hWnd, out Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle ppbData, out uint pcbData);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static void ThrowExceptionForBCryptStatus(int ntstatus) { }
        public static void ThrowExceptionForLastCrypt32Error() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static void ThrowExceptionForNCryptStatus(int ntstatus) { }
    }
    internal static partial class WeakReferenceHelpers
    {
        public static T GetSharedInstance<T>(ref System.WeakReference<T> weakReference, System.Func<T> factory) where T : class, System.IDisposable { throw null; }
    }
}
namespace Microsoft.AspNetCore.Cryptography.Cng
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct BCryptBuffer
    {
        public uint cbBuffer; // Length of buffer, in bytes
        public BCryptKeyDerivationBufferType BufferType; // Buffer type
        public System.IntPtr pvBuffer; // Pointer to buffer
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe partial struct BCryptBufferDesc
    {
        public uint ulVersion; // Version number
        public uint cBuffers; // Number of buffers
        public BCryptBuffer* pBuffers; // Pointer to array of buffers
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void Initialize(ref Microsoft.AspNetCore.Cryptography.Cng.BCryptBufferDesc bufferDesc) { }
    }
    [System.FlagsAttribute]
    internal enum BCryptEncryptFlags
    {
        BCRYPT_BLOCK_PADDING = 1,
    }
    [System.FlagsAttribute]
    internal enum BCryptGenRandomFlags
    {
        BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 1,
        BCRYPT_USE_SYSTEM_PREFERRED_RNG = 2,
    }
    internal enum BCryptKeyDerivationBufferType
    {
        KDF_HASH_ALGORITHM = 0,
        KDF_SECRET_PREPEND = 1,
        KDF_SECRET_APPEND = 2,
        KDF_HMAC_KEY = 3,
        KDF_TLS_PRF_LABEL = 4,
        KDF_TLS_PRF_SEED = 5,
        KDF_SECRET_HANDLE = 6,
        KDF_TLS_PRF_PROTOCOL = 7,
        KDF_ALGORITHMID = 8,
        KDF_PARTYUINFO = 9,
        KDF_PARTYVINFO = 10,
        KDF_SUPPPUBINFO = 11,
        KDF_SUPPPRIVINFO = 12,
        KDF_LABEL = 13,
        KDF_CONTEXT = 14,
        KDF_SALT = 15,
        KDF_ITERATION_COUNT = 16,
    }
    internal static partial class BCryptUtil
    {
        public unsafe static void GenRandom(byte* pbBuffer, uint cbBuffer) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe partial struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
    {
        public uint cbSize;
        public uint dwInfoVersion;
        public byte* pbNonce;
        public uint cbNonce;
        public byte* pbAuthData;
        public uint cbAuthData;
        public byte* pbTag;
        public uint cbTag;
        public byte* pbMacContext;
        public uint cbMacContext;
        public uint cbAAD;
        public ulong cbData;
        public uint dwFlags;
        public static void Init(out Microsoft.AspNetCore.Cryptography.Cng.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO info) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct BCRYPT_KEY_LENGTHS_STRUCT
    {
        // MSDN says these fields represent the key length in bytes.
        // It's wrong: these key lengths are all actually in bits.
        internal uint dwMinLength;
        internal uint dwMaxLength;
        internal uint dwIncrement;
        public void EnsureValidKeyLength(uint keyLengthInBits) { }
    }
    internal static partial class CachedAlgorithmHandles
    {
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle AES_CBC { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle AES_GCM { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle HMAC_SHA1 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle HMAC_SHA256 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle HMAC_SHA512 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle PBKDF2 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle SHA1 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle SHA256 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle SHA512 { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle SP800_108_CTR_HMAC { get { throw null; } }
    }
    [System.FlagsAttribute]
    internal enum NCryptEncryptFlags
    {
        NCRYPT_NO_PADDING_FLAG = 1,
        NCRYPT_PAD_PKCS1_FLAG = 2,
        NCRYPT_PAD_OAEP_FLAG = 4,
        NCRYPT_PAD_PSS_FLAG = 8,
        NCRYPT_SILENT_FLAG = 64,
    }
    internal static partial class OSVersionUtil
    {
        public static bool IsWindows() { throw null; }
        public static bool IsWindows8OrLater() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Cryptography.Internal
{
    internal static partial class Resources
    {
        internal static string BCryptAlgorithmHandle_ProviderNotFound { get { throw null; } }
        internal static string BCRYPT_KEY_LENGTHS_STRUCT_InvalidKeyLength { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string Platform_Windows7Required { get { throw null; } }
        internal static string Platform_Windows8Required { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string FormatBCryptAlgorithmHandle_ProviderNotFound(object p0) { throw null; }
        internal static string FormatBCRYPT_KEY_LENGTHS_STRUCT_InvalidKeyLength(object p0, object p1, object p2, object p3) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
    internal sealed partial class BCryptAlgorithmHandle : Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHandle
    {
        public Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle CreateHash() { throw null; }
        public unsafe Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle CreateHmac(byte* pbKey, uint cbKey) { throw null; }
        public unsafe Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptKeyHandle GenerateSymmetricKey(byte* pbSecret, uint cbSecret) { throw null; }
        public string GetAlgorithmName() { throw null; }
        public uint GetCipherBlockLength() { throw null; }
        public uint GetHashBlockLength() { throw null; }
        public uint GetHashDigestLength() { throw null; }
        public Microsoft.AspNetCore.Cryptography.Cng.BCRYPT_KEY_LENGTHS_STRUCT GetSupportedKeyLengths() { throw null; }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle OpenAlgorithmHandle(string algorithmId, string implementation = null, bool hmac = false) { throw null; }
        protected override bool ReleaseHandle() { throw null; }
        public void SetChainingMode(string chainingMode) { }
    }
    internal abstract partial class BCryptHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected BCryptHandle() : base (default(bool)) { }
        protected unsafe uint GetProperty(string pszProperty, void* pbOutput, uint cbOutput) { throw null; }
        protected unsafe void SetProperty(string pszProperty, void* pbInput, uint cbInput) { }
    }
    internal sealed partial class BCryptHashHandle : Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHandle
    {
        public Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHashHandle DuplicateHash() { throw null; }
        public unsafe void HashData(byte* pbInput, uint cbInput, byte* pbHashDigest, uint cbHashDigest) { }
        protected override bool ReleaseHandle() { throw null; }
        internal void SetAlgorithmProviderHandle(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle algProviderHandle) { }
    }
    internal sealed partial class BCryptKeyHandle : Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptHandle
    {
        protected override bool ReleaseHandle() { throw null; }
        internal void SetAlgorithmProviderHandle(Microsoft.AspNetCore.Cryptography.SafeHandles.BCryptAlgorithmHandle algProviderHandle) { }
    }
    internal partial class LocalAllocHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected LocalAllocHandle() : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
    internal sealed partial class NCryptDescriptorHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private NCryptDescriptorHandle() : base (default(bool)) { }
        public string GetProtectionDescriptorRuleString() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
    }
    internal sealed partial class SafeLibraryHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLibraryHandle() : base (default(bool)) { }
        public bool DoesProcExist(string lpProcName) { throw null; }
        public void ForbidUnload() { }
        public string FormatMessage(int messageId) { throw null; }
        public TDelegate GetProcAddress<TDelegate>(string lpProcName, bool throwIfNotFound = true) where TDelegate : class { throw null; }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.SafeLibraryHandle Open(string filename) { throw null; }
        protected override bool ReleaseHandle() { throw null; }
    }
    internal sealed partial class SecureLocalAllocHandle : Microsoft.AspNetCore.Cryptography.SafeHandles.LocalAllocHandle
    {
        public System.IntPtr Length { get { throw null; } }
        public static Microsoft.AspNetCore.Cryptography.SafeHandles.SecureLocalAllocHandle Allocate(System.IntPtr cb) { throw null; }
        public Microsoft.AspNetCore.Cryptography.SafeHandles.SecureLocalAllocHandle Duplicate() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
    }
}
