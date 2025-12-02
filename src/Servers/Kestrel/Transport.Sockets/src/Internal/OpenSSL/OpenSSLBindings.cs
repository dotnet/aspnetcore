// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.OpenSSL;

/// <summary>
/// Minimal OpenSSL bindings for direct socket-to-TLS integration.
/// This is a foundation for zero-copy TLS processing.
/// </summary>
internal static class OpenSSLBindings
{
    private const string LibName = "libssl";

    // OpenSSL function stubs - to be implemented based on libssl availability
    // These are placeholders for the actual OpenSSL integration

    /// <summary>
    /// SSL_new - Create a new SSL connection structure.
    /// </summary>
    [DllImport(LibName)]
    internal static extern IntPtr SSL_new(IntPtr ctx);

    /// <summary>
    /// SSL_free - Free an SSL connection structure.
    /// </summary>
    [DllImport(LibName)]
    internal static extern void SSL_free(IntPtr ssl);

    /// <summary>
    /// SSL_set_fd - Associate an OpenSSL connection with a file descriptor.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_set_fd(IntPtr ssl, int fd);

    /// <summary>
    /// SSL_accept - Perform SSL handshake as server.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_accept(IntPtr ssl);

    /// <summary>
    /// SSL_read - Read decrypted data from OpenSSL.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_read(IntPtr ssl, byte[] buf, int num);

    /// <summary>
    /// SSL_write - Write data to OpenSSL for encryption.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_write(IntPtr ssl, byte[] buf, int num);

    /// <summary>
    /// SSL_get_error - Get error code from last SSL operation.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_get_error(IntPtr ssl, int ret);

    /// <summary>
    /// SSL_shutdown - Perform SSL shutdown.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_shutdown(IntPtr ssl);

    /// <summary>
    /// SSL_CTX_new - Create a new SSL context.
    /// </summary>
    [DllImport(LibName)]
    internal static extern IntPtr SSL_CTX_new(IntPtr method);

    /// <summary>
    /// SSL_CTX_free - Free an SSL context.
    /// </summary>
    [DllImport(LibName)]
    internal static extern void SSL_CTX_free(IntPtr ctx);

    /// <summary>
    /// TLS_server_method - Get the server-side TLS method.
    /// </summary>
    [DllImport(LibName)]
    internal static extern IntPtr TLS_server_method();

    /// <summary>
    /// SSL_CTX_use_certificate_file - Load server certificate from file.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_CTX_use_certificate_file(IntPtr ctx, string file, int type);

    /// <summary>
    /// SSL_CTX_use_PrivateKey_file - Load private key from file.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_CTX_use_PrivateKey_file(IntPtr ctx, string file, int type);

    /// <summary>
    /// SSL_get_state - Get the current state of the SSL connection.
    /// </summary>
    [DllImport(LibName)]
    internal static extern int SSL_get_state(IntPtr ssl);

    /// <summary>
    /// ERR_get_error - Get error from error queue.
    /// </summary>
    [DllImport(LibName)]
    internal static extern ulong ERR_get_error();

    /// <summary>
    /// ERR_error_string - Convert error code to string.
    /// </summary>
    [DllImport(LibName)]
    internal static extern IntPtr ERR_error_string(ulong err, IntPtr buf);
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

/// <summary>
/// File format types
/// </summary>
internal enum OpenSSLFileFormat
{
    SSL_FILETYPE_PEM = 1,
    SSL_FILETYPE_ASN1 = 2,
}

/// <summary>
/// Managed wrapper for an OpenSSL SSL_CTX* (SSL context)
/// </summary>
internal sealed class OpenSSLContext : IDisposable
{
    private IntPtr _ctx;
    private bool _disposed;

    public IntPtr Handle => _ctx;

    public OpenSSLContext(IntPtr ctx)
    {
        _ctx = ctx;
    }

    public void Dispose()
    {
        if (!_disposed && _ctx != IntPtr.Zero)
        {
            try
            {
                OpenSSLBindings.SSL_CTX_free(_ctx);
            }
            catch
            {
                // Ignore errors during cleanup
            }

            _disposed = true;
            _ctx = IntPtr.Zero;
        }
    }
}

/// <summary>
/// Managed wrapper for an OpenSSL SSL* (SSL connection)
/// </summary>
internal sealed class OpenSSLConnection : IDisposable
{
    private IntPtr _ssl;
    private bool _disposed;

    public IntPtr Handle => _ssl;

    public OpenSSLConnection(IntPtr ssl)
    {
        _ssl = ssl;
    }

    public void Dispose()
    {
        if (!_disposed && _ssl != IntPtr.Zero)
        {
            try
            {
                OpenSSLBindings.SSL_free(_ssl);
            }
            catch
            {
                // Ignore errors during cleanup
            }

            _disposed = true;
            _ssl = IntPtr.Zero;
        }
    }
}
