// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

/// <summary>
/// OpenSSL interop definitions for direct SSL/TLS operations.
/// This allows us to bypass SslStream and use non-blocking SSL_do_handshake.
/// </summary>
internal static unsafe partial class OpenSsl
{
    private const string LibSsl = "libssl.so.3"; // OpenSSL 3.x on Linux
    private const string LibCrypto = "libcrypto.so.3";

    // SSL/TLS Protocol versions
    public const int TLS1_2_VERSION = 0x0303;
    public const int TLS1_3_VERSION = 0x0304;

    // SSL_do_handshake return codes
    public const int SSL_ERROR_NONE = 0;
    public const int SSL_ERROR_WANT_READ = 2;
    public const int SSL_ERROR_WANT_WRITE = 3;
    public const int SSL_ERROR_SYSCALL = 5;
    public const int SSL_ERROR_SSL = 1;

    // File types for SSL_CTX_use_certificate_file
    public const int SSL_FILETYPE_PEM = 1;

    #region SSL Context Management

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr TLS_server_method();

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr SSL_CTX_new(IntPtr method);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SSL_CTX_free(IntPtr ctx);

    [LibraryImport(LibSsl, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SSL_CTX_use_certificate_file(IntPtr ctx, string file, int type);

    [LibraryImport(LibSsl, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SSL_CTX_use_PrivateKey_file(IntPtr ctx, string file, int type);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_CTX_check_private_key(IntPtr ctx);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial long SSL_CTX_set_options(IntPtr ctx, long options);

    #endregion

    #region SSL Session Management

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr SSL_new(IntPtr ctx);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SSL_free(IntPtr ssl);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_set_fd(IntPtr ssl, int fd);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_accept(IntPtr ssl);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SSL_set_accept_state(IntPtr ssl);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_do_handshake(IntPtr ssl);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_get_error(IntPtr ssl, int ret);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_read(IntPtr ssl, byte* buf, int num);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_write(IntPtr ssl, byte* buf, int num);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int SSL_shutdown(IntPtr ssl);

    #endregion

    #region OpenSSL Initialization

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int OPENSSL_init_ssl(ulong opts, IntPtr settings);

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int OPENSSL_init_crypto(ulong opts, IntPtr settings);

    #endregion

    #region Error Handling

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial ulong ERR_get_error();

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr ERR_error_string(ulong e, byte* buf);

    public static string GetLastErrorString()
    {
        var error = ERR_get_error();
        if (error == 0)
        {
            return "No error";
        }

        byte* buffer = stackalloc byte[256];
        ERR_error_string(error, buffer);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? "Unknown error";
    }

    #endregion

    #region BIO Operations

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr BIO_s_mem();

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr BIO_new(IntPtr type);

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void BIO_free(IntPtr bio);

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SSL_set_bio(IntPtr ssl, IntPtr rbio, IntPtr wbio);

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int BIO_read(IntPtr bio, byte* buf, int len);

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int BIO_write(IntPtr bio, byte* buf, int len);

    [LibraryImport(LibCrypto)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int BIO_ctrl_pending(IntPtr bio);

    #endregion

    #region Helper Methods

    public static void Initialize()
    {
        // Initialize OpenSSL library
        OPENSSL_init_ssl(0, IntPtr.Zero);
        OPENSSL_init_crypto(0, IntPtr.Zero);
    }

    #endregion
}
