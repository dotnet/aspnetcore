// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl.Internal.OpenSSL;

/// <summary>
/// Managed wrapper for an OpenSSL SSL* (SSL connection).
/// </summary>
internal sealed class OpenSSLConnection : IDisposable
{
    private IntPtr _ssl;
    private bool _disposed;

    public IntPtr Handle => _ssl;

    public OpenSSLConnection(IntPtr ssl)
    {
        if (ssl == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid SSL handle", nameof(ssl));
        }
        _ssl = ssl;
    }

    /// <summary>
    /// Creates a new SSL connection from the context.
    /// </summary>
    public static OpenSSLConnection Create(OpenSSLContext context)
    {
        var ssl = OpenSSLBindings.SSL_new(context.Handle);
        if (ssl == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create SSL connection: {OpenSSLBindings.GetLastErrorString()}");
        }
        return new OpenSSLConnection(ssl);
    }

    /// <summary>
    /// Associates the SSL connection with a file descriptor (socket).
    /// </summary>
    public void SetFileDescriptor(int fd)
    {
        if (OpenSSLBindings.SSL_set_fd(_ssl, fd) != 1)
        {
            throw new InvalidOperationException($"Failed to set SSL file descriptor: {OpenSSLBindings.GetLastErrorString()}");
        }
    }

    /// <summary>
    /// Performs the SSL handshake as a server.
    /// </summary>
    /// <returns>True if handshake completed, false if it needs to be retried.</returns>
    public int Accept()
    {
        return OpenSSLBindings.SSL_accept(_ssl);
    }

    /// <summary>
    /// Gets the error code for the last SSL operation.
    /// </summary>
    public OpenSSLError GetError(int ret)
    {
        return (OpenSSLError)OpenSSLBindings.SSL_get_error(_ssl, ret);
    }

    /// <summary>
    /// Shuts down the SSL connection.
    /// </summary>
    public int Shutdown()
    {
        return OpenSSLBindings.SSL_shutdown(_ssl);
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

    ~OpenSSLConnection()
    {
        Dispose();
    }
}
