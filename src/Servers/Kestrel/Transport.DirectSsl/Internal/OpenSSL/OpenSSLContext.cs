// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl.Internal.OpenSSL;

/// <summary>
/// Managed wrapper for an OpenSSL SSL_CTX* (SSL context).
/// Thread-safe - can be shared across connections.
/// </summary>
internal sealed class OpenSSLContext : IDisposable
{
    private IntPtr _ctx;
    private bool _disposed;

    public IntPtr Handle => _ctx;

    /// <summary>
    /// Creates a new OpenSSL context from an existing handle.
    /// </summary>
    public OpenSSLContext(IntPtr ctx)
    {
        if (ctx == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid SSL context handle", nameof(ctx));
        }
        _ctx = ctx;
    }

    /// <summary>
    /// Creates a new OpenSSL context with certificate and key files.
    /// </summary>
    public OpenSSLContext(string certPath, string keyPath)
    {
        if (string.IsNullOrEmpty(certPath))
            throw new ArgumentNullException(nameof(certPath));
        if (string.IsNullOrEmpty(keyPath))
            throw new ArgumentNullException(nameof(keyPath));

        // Create SSL context with TLS server method
        var method = OpenSSLBindings.TLS_server_method();
        if (method == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create TLS server method");

        _ctx = OpenSSLBindings.SSL_CTX_new(method);
        if (_ctx == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to create SSL context: {OpenSSLBindings.GetLastErrorString()}");

        try
        {
            // Load certificate
            if (OpenSSLBindings.SSL_CTX_use_certificate_file(_ctx, certPath, OpenSSLBindings.SSL_FILETYPE_PEM) <= 0)
            {
                throw new InvalidOperationException($"Failed to load certificate from {certPath}: {OpenSSLBindings.GetLastErrorString()}");
            }

            // Load private key
            if (OpenSSLBindings.SSL_CTX_use_PrivateKey_file(_ctx, keyPath, OpenSSLBindings.SSL_FILETYPE_PEM) <= 0)
            {
                throw new InvalidOperationException($"Failed to load private key from {keyPath}: {OpenSSLBindings.GetLastErrorString()}");
            }

            // Verify private key matches certificate
            if (OpenSSLBindings.SSL_CTX_check_private_key(_ctx) <= 0)
            {
                throw new InvalidOperationException($"Private key does not match certificate: {OpenSSLBindings.GetLastErrorString()}");
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ctx != IntPtr.Zero)
            {
                try
                {
                    OpenSSLBindings.SSL_CTX_free(_ctx);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
                _ctx = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~OpenSSLContext()
    {
        Dispose();
    }
}
