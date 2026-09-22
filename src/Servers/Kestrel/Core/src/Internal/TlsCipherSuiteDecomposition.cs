// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;

// These are the legacy properties ITlsHandshakeFeature still exposes; the runtime file this
// table is copied from disables the same warning.
#pragma warning disable SYSLIB0058

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

/// <summary>
/// Decomposes a <see cref="TlsCipherSuite"/> into the legacy algorithm/strength values that
/// <c>ITlsHandshakeFeature</c> exposes.
///
/// <para><see cref="SslStream"/> gets these from the runtime's own table, which is internal, so
/// this is a copy of the generated mapping in dotnet/runtime
/// (<c>System.Net.Security/src/System/Net/Security/SslConnectionInfo.Unix.cs</c>). The sans-IO
/// <c>TlsSession</c> surfaces only <c>NegotiatedCipherSuite</c>, so without this the feature
/// would have to report <see cref="CipherAlgorithmType.None"/> and zero strengths - a visible
/// behaviour regression against the SslStream path.</para>
///
/// <para>This duplication is deliberate and temporary: delete it in favour of the runtime API
/// once one exists for turning a cipher suite into its component algorithms. The values here
/// are legacy (superseded by <c>NegotiatedCipherSuite</c>) and are only needed for
/// compatibility.</para>
/// </summary>
internal static class TlsCipherSuiteDecomposition
{
    private static ReadOnlySpan<int> KeyExchangeAlgorithms =>
        [(int)ExchangeAlgorithmType.None, (int)ExchangeAlgorithmType.RsaSign, (int)ExchangeAlgorithmType.RsaKeyX, (int)ExchangeAlgorithmType.DiffieHellman];

    private static ReadOnlySpan<int> CipherAlgorithms =>
        [(int)CipherAlgorithmType.None, (int)CipherAlgorithmType.Null, (int)CipherAlgorithmType.Des, (int)CipherAlgorithmType.Rc2, (int)CipherAlgorithmType.TripleDes, (int)CipherAlgorithmType.Aes128, (int)CipherAlgorithmType.Aes192, (int)CipherAlgorithmType.Aes256, (int)CipherAlgorithmType.Aes, (int)CipherAlgorithmType.Rc4];

    private static ReadOnlySpan<int> CipherKeySizes => [0, 40, 56, 128, 168, 256];

    private static ReadOnlySpan<int> HashAlgorithms =>
        [(int)HashAlgorithmType.None, (int)HashAlgorithmType.Md5, (int)HashAlgorithmType.Sha1, (int)HashAlgorithmType.Sha256, (int)HashAlgorithmType.Sha384, (int)HashAlgorithmType.Sha512];

    private static ReadOnlySpan<int> HashKeySizes => [0, 128, 160, 256, 384, 512];

    /// <summary>
    /// Maps <paramref name="cipherSuite"/> to its component algorithms. Unknown suites yield
    /// <see cref="CipherAlgorithmType.None"/> and zero strengths, matching what the runtime
    /// table does for suites it has no entry for.
    /// </summary>
    public static void Decompose(
        TlsCipherSuite cipherSuite,
        out CipherAlgorithmType cipherAlgorithm,
        out int cipherStrength,
        out HashAlgorithmType hashAlgorithm,
        out int hashStrength,
        out ExchangeAlgorithmType keyExchangeAlgorithm,
        out int keyExchangeStrength)
    {
        var data = GetPackedData(cipherSuite);

        keyExchangeAlgorithm = (ExchangeAlgorithmType)KeyExchangeAlgorithms[(data >> 12) & 0xF];
        cipherAlgorithm = (CipherAlgorithmType)CipherAlgorithms[(data >> 8) & 0xF];
        cipherStrength = CipherKeySizes[(data >> 4) & 0xF];
        hashAlgorithm = (HashAlgorithmType)HashAlgorithms[data & 0xF];
        hashStrength = HashKeySizes[data & 0xF];

        // The runtime reports 0 here as well; the size is not derivable from the suite alone.
        keyExchangeStrength = 0;
    }

    private static int GetPackedData(TlsCipherSuite cipherSuite)
    {
        switch (cipherSuite)
        {
                    case TlsCipherSuite.TLS_NULL_WITH_NULL_NULL: return 0 << 12 | 1 << 8 | 0 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_NULL_MD5: return 2 << 12 | 1 << 8 | 0 << 4 | 1;
                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA: return 2 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5: return 2 << 12 | 9 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5: return 2 << 12 | 9 << 8 | 3 << 4 | 1;
                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA: return 2 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5: return 2 << 12 | 3 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA: return 2 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA: return 2 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA: return 2 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA: return 2 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA: return 3 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA: return 3 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA: return 3 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA: return 3 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA: return 3 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA: return 3 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA: return 3 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA: return 3 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5: return 3 << 12 | 9 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5: return 3 << 12 | 9 << 8 | 3 << 4 | 1;
                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA: return 3 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA: return 3 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA: return 0 << 12 | 2 << 8 | 2 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA: return 0 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA: return 0 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA: return 0 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5: return 0 << 12 | 2 << 8 | 2 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5: return 0 << 12 | 4 << 8 | 4 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5: return 0 << 12 | 9 << 8 | 3 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5: return 0 << 12 | 0 << 8 | 3 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA: return 0 << 12 | 2 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA: return 0 << 12 | 3 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA: return 0 << 12 | 9 << 8 | 1 << 4 | 2;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5: return 0 << 12 | 2 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5: return 0 << 12 | 3 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5: return 0 << 12 | 9 << 8 | 1 << 4 | 1;
                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA: return 0 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA: return 2 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA: return 2 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA: return 2 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256: return 2 << 12 | 1 << 8 | 0 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256: return 2 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256: return 2 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA: return 2 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256: return 3 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256: return 3 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256: return 3 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256: return 3 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256: return 3 << 12 | 7 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA: return 2 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA: return 3 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA: return 3 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA: return 3 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA: return 3 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA: return 3 << 12 | 0 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA: return 0 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA: return 0 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA: return 0 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA: return 0 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA: return 2 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA: return 2 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA: return 2 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA: return 2 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA: return 2 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA: return 3 << 12 | 0 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256: return 2 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384: return 2 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256: return 2 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384: return 2 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384: return 0 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256: return 0 << 12 | 1 << 8 | 0 << 4 | 3;
                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384: return 0 << 12 | 1 << 8 | 0 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256: return 3 << 12 | 1 << 8 | 0 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384: return 3 << 12 | 1 << 8 | 0 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256: return 2 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384: return 2 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256: return 2 << 12 | 1 << 8 | 0 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384: return 2 << 12 | 1 << 8 | 0 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 2 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 3;
                    case TlsCipherSuite.TLS_AES_128_GCM_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_AES_256_GCM_SHA384: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256: return 0 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_AES_128_CCM_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_AES_128_CCM_8_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA: return 0 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA: return 0 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA: return 0 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA: return 0 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA: return 0 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA: return 0 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA: return 0 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA: return 0 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA: return 0 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA: return 3 << 12 | 9 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA: return 3 << 12 | 4 << 8 | 4 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA: return 3 << 12 | 5 << 8 | 3 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA: return 3 << 12 | 7 << 8 | 5 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA: return 3 << 12 | 1 << 8 | 0 << 4 | 2;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256: return 3 << 12 | 1 << 8 | 0 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384: return 3 << 12 | 1 << 8 | 0 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256: return 0 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384: return 0 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256: return 0 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384: return 0 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 0 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 0 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 0 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 0 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 2 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 2 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 3 << 12 | 0 << 8 | 3 << 4 | 3;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 3 << 12 | 0 << 8 | 5 << 4 | 4;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM: return 2 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM: return 2 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8: return 2 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8: return 2 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256: return 0 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384: return 0 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256: return 0 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return 3 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256: return 2 << 12 | 0 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384: return 3 << 12 | 7 << 8 | 5 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256: return 3 << 12 | 5 << 8 | 3 << 4 | 0;
                    default: return 0;
        }
    }
}
