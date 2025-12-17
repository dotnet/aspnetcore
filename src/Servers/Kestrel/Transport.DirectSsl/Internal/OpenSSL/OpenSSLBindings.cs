// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl.Internal.OpenSSL;

/// <summary>
/// Minimal OpenSSL bindings for direct socket-to-TLS integration.
/// This is a foundation for zero-copy TLS processing.
/// </summary>
internal static unsafe partial class OpenSSLBindings
{
    private const string LibSsl = "libssl";
    private const string LibCrypto = "libcrypto";

    // SSL_FILETYPE constants
    internal const int SSL_FILETYPE_PEM = 1;
    internal const int SSL_FILETYPE_ASN1 = 2;

    /// <summary>
    /// SSL_new - Create a new SSL connection structure.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial IntPtr SSL_new(IntPtr ctx);

    /// <summary>
    /// SSL_free - Free an SSL connection structure.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial void SSL_free(IntPtr ssl);

    /// <summary>
    /// SSL_set_fd - Associate an OpenSSL connection with a file descriptor.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_set_fd(IntPtr ssl, int fd);

    /// <summary>
    /// SSL_accept - Perform SSL handshake as server.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_accept(IntPtr ssl);

    /// <summary>
    /// SSL_read - Read decrypted data from OpenSSL.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_read(IntPtr ssl, byte* buf, int num);

    /// <summary>
    /// SSL_write - Write data to OpenSSL for encryption.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_write(IntPtr ssl, byte* buf, int num);

    /// <summary>
    /// SSL_get_error - Get error code from last SSL operation.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_get_error(IntPtr ssl, int ret);

    /// <summary>
    /// SSL_shutdown - Perform SSL shutdown.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_shutdown(IntPtr ssl);

    /// <summary>
    /// SSL_CTX_new - Create a new SSL context.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial IntPtr SSL_CTX_new(IntPtr method);

    /// <summary>
    /// SSL_CTX_free - Free an SSL context.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial void SSL_CTX_free(IntPtr ctx);

    /// <summary>
    /// TLS_server_method - Get the server-side TLS method.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial IntPtr TLS_server_method();

    /// <summary>
    /// SSL_CTX_use_certificate_file - Load server certificate from file.
    /// </summary>
    [LibraryImport(LibSsl, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SSL_CTX_use_certificate_file(IntPtr ctx, string file, int type);

    /// <summary>
    /// SSL_CTX_use_PrivateKey_file - Load private key from file.
    /// </summary>
    [LibraryImport(LibSsl, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SSL_CTX_use_PrivateKey_file(IntPtr ctx, string file, int type);

    /// <summary>
    /// SSL_CTX_check_private_key - Verify private key matches certificate.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_CTX_check_private_key(IntPtr ctx);

    /// <summary>
    /// SSL_get_state - Get the current state of the SSL connection.
    /// </summary>
    [LibraryImport(LibSsl)]
    internal static partial int SSL_get_state(IntPtr ssl);

    /// <summary>
    /// ERR_get_error - Get error from error queue.
    /// </summary>
    [LibraryImport(LibCrypto)]
    internal static partial ulong ERR_get_error();

    /// <summary>
    /// ERR_error_string_n - Convert error code to string.
    /// </summary>
    [LibraryImport(LibCrypto)]
    internal static partial void ERR_error_string_n(ulong e, byte* buf, nuint len);

    /// <summary>
    /// Gets the last OpenSSL error as a string.
    /// </summary>
    internal static string GetLastErrorString()
    {
        var error = ERR_get_error();
        if (error == 0)
        {
            return "No error";
        }

        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* ptr = buffer)
        {
            ERR_error_string_n(error, ptr, (nuint)buffer.Length);
            var length = buffer.IndexOf((byte)0);
            if (length < 0) length = buffer.Length;
            return System.Text.Encoding.UTF8.GetString(buffer[..length]);
        }
    }
}

/// <summary>
/// OpenSSL error codes
/// </summary>
internal enum OpenSSLError
{
    SSL_ERROR_NONE = 0,
    SSL_ERROR_SSL = 1,
    SSL_ERROR_WANT_READ = 2,
    SSL_ERROR_WANT_WRITE = 3,
    SSL_ERROR_WANT_X509_LOOKUP = 4,
    SSL_ERROR_SYSCALL = 5,
    SSL_ERROR_ZERO_RETURN = 6,
    SSL_ERROR_WANT_CONNECT = 7,
    SSL_ERROR_WANT_ACCEPT = 8,
}

/// <summary>
/// OpenSSL connection states
/// </summary>
internal enum OpenSSLState
{
    SSL_ST_INIT = 0x0000,
    SSL_ST_BEFORE = 0x4000,
    SSL_ST_OK = 0x03,
    SSL_ST_ERR = 0x05,
}
