// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cryptography;

// The majority of these are from bcrypt.h
internal static class Constants
{
    internal const int MAX_STACKALLOC_BYTES = 256; // greatest number of bytes that we'll ever allow to stackalloc in a single frame

    // BCrypt(Import/Export)Key BLOB types
    internal const string BCRYPT_OPAQUE_KEY_BLOB = "OpaqueKeyBlob";
    internal const string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";
    internal const string BCRYPT_AES_WRAP_KEY_BLOB = "Rfc3565KeyWrapBlob";

    // Microsoft built-in providers
    internal const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";
    internal const string MS_PLATFORM_CRYPTO_PROVIDER = "Microsoft Platform Crypto Provider";

    // Common algorithm identifiers
    internal const string BCRYPT_RSA_ALGORITHM = "RSA";
    internal const string BCRYPT_RSA_SIGN_ALGORITHM = "RSA_SIGN";
    internal const string BCRYPT_DH_ALGORITHM = "DH";
    internal const string BCRYPT_DSA_ALGORITHM = "DSA";
    internal const string BCRYPT_RC2_ALGORITHM = "RC2";
    internal const string BCRYPT_RC4_ALGORITHM = "RC4";
    internal const string BCRYPT_AES_ALGORITHM = "AES";
    internal const string BCRYPT_DES_ALGORITHM = "DES";
    internal const string BCRYPT_DESX_ALGORITHM = "DESX";
    internal const string BCRYPT_3DES_ALGORITHM = "3DES";
    internal const string BCRYPT_3DES_112_ALGORITHM = "3DES_112";
    internal const string BCRYPT_MD2_ALGORITHM = "MD2";
    internal const string BCRYPT_MD4_ALGORITHM = "MD4";
    internal const string BCRYPT_MD5_ALGORITHM = "MD5";
    internal const string BCRYPT_SHA1_ALGORITHM = "SHA1";
    internal const string BCRYPT_SHA256_ALGORITHM = "SHA256";
    internal const string BCRYPT_SHA384_ALGORITHM = "SHA384";
    internal const string BCRYPT_SHA512_ALGORITHM = "SHA512";
    internal const string BCRYPT_AES_GMAC_ALGORITHM = "AES-GMAC";
    internal const string BCRYPT_AES_CMAC_ALGORITHM = "AES-CMAC";
    internal const string BCRYPT_ECDSA_P256_ALGORITHM = "ECDSA_P256";
    internal const string BCRYPT_ECDSA_P384_ALGORITHM = "ECDSA_P384";
    internal const string BCRYPT_ECDSA_P521_ALGORITHM = "ECDSA_P521";
    internal const string BCRYPT_ECDH_P256_ALGORITHM = "ECDH_P256";
    internal const string BCRYPT_ECDH_P384_ALGORITHM = "ECDH_P384";
    internal const string BCRYPT_ECDH_P521_ALGORITHM = "ECDH_P521";
    internal const string BCRYPT_RNG_ALGORITHM = "RNG";
    internal const string BCRYPT_RNG_FIPS186_DSA_ALGORITHM = "FIPS186DSARNG";
    internal const string BCRYPT_RNG_DUAL_EC_ALGORITHM = "DUALECRNG";
    internal const string BCRYPT_SP800108_CTR_HMAC_ALGORITHM = "SP800_108_CTR_HMAC";
    internal const string BCRYPT_SP80056A_CONCAT_ALGORITHM = "SP800_56A_CONCAT";
    internal const string BCRYPT_PBKDF2_ALGORITHM = "PBKDF2";
    internal const string BCRYPT_CAPI_KDF_ALGORITHM = "CAPI_KDF";

    // BCryptGetProperty strings
    internal const string BCRYPT_OBJECT_LENGTH = "ObjectLength";
    internal const string BCRYPT_ALGORITHM_NAME = "AlgorithmName";
    internal const string BCRYPT_PROVIDER_HANDLE = "ProviderHandle";
    internal const string BCRYPT_CHAINING_MODE = "ChainingMode";
    internal const string BCRYPT_BLOCK_LENGTH = "BlockLength";
    internal const string BCRYPT_KEY_LENGTH = "KeyLength";
    internal const string BCRYPT_KEY_OBJECT_LENGTH = "KeyObjectLength";
    internal const string BCRYPT_KEY_STRENGTH = "KeyStrength";
    internal const string BCRYPT_KEY_LENGTHS = "KeyLengths";
    internal const string BCRYPT_BLOCK_SIZE_LIST = "BlockSizeList";
    internal const string BCRYPT_EFFECTIVE_KEY_LENGTH = "EffectiveKeyLength";
    internal const string BCRYPT_HASH_LENGTH = "HashDigestLength";
    internal const string BCRYPT_HASH_OID_LIST = "HashOIDList";
    internal const string BCRYPT_PADDING_SCHEMES = "PaddingSchemes";
    internal const string BCRYPT_SIGNATURE_LENGTH = "SignatureLength";
    internal const string BCRYPT_HASH_BLOCK_LENGTH = "HashBlockLength";
    internal const string BCRYPT_AUTH_TAG_LENGTH = "AuthTagLength";
    internal const string BCRYPT_PRIMITIVE_TYPE = "PrimitiveType";
    internal const string BCRYPT_IS_KEYED_HASH = "IsKeyedHash";
    internal const string BCRYPT_IS_REUSABLE_HASH = "IsReusableHash";
    internal const string BCRYPT_MESSAGE_BLOCK_LENGTH = "MessageBlockLength";

    // Property Strings
    internal const string BCRYPT_CHAIN_MODE_NA = "ChainingModeN/A";
    internal const string BCRYPT_CHAIN_MODE_CBC = "ChainingModeCBC";
    internal const string BCRYPT_CHAIN_MODE_ECB = "ChainingModeECB";
    internal const string BCRYPT_CHAIN_MODE_CFB = "ChainingModeCFB";
    internal const string BCRYPT_CHAIN_MODE_CCM = "ChainingModeCCM";
    internal const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
}
