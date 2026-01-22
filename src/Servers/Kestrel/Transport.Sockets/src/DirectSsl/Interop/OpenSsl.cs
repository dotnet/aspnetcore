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
    
    // SSL_CTX modes (set via SSL_CTX_set_mode)
    public const long SSL_MODE_ENABLE_PARTIAL_WRITE = 0x00000001;
    public const long SSL_MODE_ACCEPT_MOVING_WRITE_BUFFER = 0x00000002;
    public const long SSL_MODE_AUTO_RETRY = 0x00000004;
    public const long SSL_MODE_RELEASE_BUFFERS = 0x00000010;

    // SSL_CTX session cache modes (for SSL_CTX_set_session_cache_mode)
    public const int SSL_SESS_CACHE_OFF = 0x0000;
    public const int SSL_SESS_CACHE_CLIENT = 0x0001;
    public const int SSL_SESS_CACHE_SERVER = 0x0002;
    public const int SSL_SESS_CACHE_BOTH = SSL_SESS_CACHE_CLIENT | SSL_SESS_CACHE_SERVER;
    public const int SSL_SESS_CACHE_NO_AUTO_CLEAR = 0x0080;
    public const int SSL_SESS_CACHE_NO_INTERNAL_LOOKUP = 0x0100;
    public const int SSL_SESS_CACHE_NO_INTERNAL_STORE = 0x0200;
    public const int SSL_SESS_CACHE_NO_INTERNAL = SSL_SESS_CACHE_NO_INTERNAL_LOOKUP | SSL_SESS_CACHE_NO_INTERNAL_STORE;

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

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial long SSL_CTX_set_mode(IntPtr ctx, long mode);

    #endregion

    #region SSL Session Caching

    // SSL_CTX_ctrl command codes for session caching
    // These are used because SSL_CTX_set_session_cache_mode and SSL_CTX_sess_set_cache_size
    // are macros in OpenSSL that call SSL_CTX_ctrl
    private const int SSL_CTRL_SET_SESS_CACHE_SIZE = 42;
    private const int SSL_CTRL_SET_SESS_CACHE_MODE = 44;
    private const int SSL_CTRL_SESS_NUMBER = 20;
    private const int SSL_CTRL_SESS_HITS = 27;
    private const int SSL_CTRL_SESS_MISSES = 29;

    /// <summary>
    /// Generic SSL_CTX control function used by session caching macros.
    /// </summary>
    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial long SSL_CTX_ctrl(IntPtr ctx, int cmd, long larg, IntPtr parg);

    /// <summary>
    /// Set the session cache mode for the SSL context.
    /// For server-side session resumption, use SSL_SESS_CACHE_SERVER.
    /// This is equivalent to the SSL_CTX_set_session_cache_mode macro.
    /// </summary>
    /// <returns>The previous cache mode</returns>
    public static int SetSessionCacheMode(IntPtr ctx, int mode)
    {
        return (int)SSL_CTX_ctrl(ctx, SSL_CTRL_SET_SESS_CACHE_MODE, mode, IntPtr.Zero);
    }

    /// <summary>
    /// Set the maximum number of sessions in the cache.
    /// Default is SSL_SESSION_CACHE_MAX_SIZE_DEFAULT (1024*20).
    /// This is equivalent to the SSL_CTX_sess_set_cache_size macro.
    /// </summary>
    /// <returns>The previous cache size</returns>
    public static long SetSessionCacheSize(IntPtr ctx, long size)
    {
        return SSL_CTX_ctrl(ctx, SSL_CTRL_SET_SESS_CACHE_SIZE, size, IntPtr.Zero);
    }

    /// <summary>
    /// Set the timeout for sessions in the cache (in seconds).
    /// Default is typically 300 seconds (5 minutes).
    /// This is a real function, not a macro.
    /// </summary>
    /// <returns>The previous timeout value</returns>
    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial long SSL_CTX_set_timeout(IntPtr ctx, long t);

    /// <summary>
    /// Get the current number of sessions in the cache.
    /// Useful for monitoring cache usage.
    /// </summary>
    public static long GetSessionNumber(IntPtr ctx)
    {
        return SSL_CTX_ctrl(ctx, SSL_CTRL_SESS_NUMBER, 0, IntPtr.Zero);
    }

    /// <summary>
    /// Get the number of successful session resumptions (cache hits).
    /// </summary>
    public static long GetSessionHits(IntPtr ctx)
    {
        return SSL_CTX_ctrl(ctx, SSL_CTRL_SESS_HITS, 0, IntPtr.Zero);
    }

    /// <summary>
    /// Get the number of session cache misses.
    /// </summary>
    public static long GetSessionMisses(IntPtr ctx)
    {
        return SSL_CTX_ctrl(ctx, SSL_CTRL_SESS_MISSES, 0, IntPtr.Zero);
    }

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

    [LibraryImport(LibSsl)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SSL_set_quiet_shutdown(IntPtr ssl, int mode);

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
