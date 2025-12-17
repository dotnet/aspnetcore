// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;

/// <summary>
/// Wrapper around OpenSSL SSL_CTX.
/// Manages the SSL context lifecycle and certificate loading.
/// Thread-safe - can be shared across connections.
/// </summary>
internal sealed class SslContext : IDisposable
{
    private IntPtr _ctx;
    private bool _disposed;

    public IntPtr Handle => _ctx;

    public SslContext(string certPath, string keyPath)
    {
        if (string.IsNullOrEmpty(certPath))
        {
            throw new ArgumentNullException(nameof(certPath));
        }

        if (string.IsNullOrEmpty(keyPath))
        {
            throw new ArgumentNullException(nameof(keyPath));
        }

        // Initialize OpenSSL
        OpenSsl.Initialize();

        // Create SSL context with TLS server method
        var method = OpenSsl.TLS_server_method();
        if (method == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create TLS server method");
        }

        _ctx = OpenSsl.SSL_CTX_new(method);
        if (_ctx == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create SSL context: {OpenSsl.GetLastErrorString()}");
        }

        // Load certificate
        if (OpenSsl.SSL_CTX_use_certificate_file(_ctx, certPath, OpenSsl.SSL_FILETYPE_PEM) <= 0)
        {
            Dispose();
            throw new InvalidOperationException($"Failed to load certificate from {certPath}: {OpenSsl.GetLastErrorString()}");
        }

        // Load private key
        if (OpenSsl.SSL_CTX_use_PrivateKey_file(_ctx, keyPath, OpenSsl.SSL_FILETYPE_PEM) <= 0)
        {
            Dispose();
            throw new InvalidOperationException($"Failed to load private key from {keyPath}: {OpenSsl.GetLastErrorString()}");
        }

        // Verify private key matches certificate
        if (OpenSsl.SSL_CTX_check_private_key(_ctx) <= 0)
        {
            Dispose();
            throw new InvalidOperationException($"Private key does not match certificate: {OpenSsl.GetLastErrorString()}");
        }

        // TLS_server_method() in OpenSSL 3.x already supports TLS 1.2 and 1.3 by default
        // No need to explicitly set min/max versions

        Console.WriteLine($"[SslContext] Initialized with cert: {certPath}");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ctx != IntPtr.Zero)
            {
                OpenSsl.SSL_CTX_free(_ctx);
                _ctx = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~SslContext()
    {
        Dispose();
    }
}