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

        // Enable TLS session resumption for performance
        // This allows returning clients to skip expensive ECDHE key exchange
        ConfigureSessionCaching();

        // TLS_server_method() in OpenSSL 3.x already supports TLS 1.2 and 1.3 by default
        // No need to explicitly set min/max versions

        Console.WriteLine($"[SslContext] Initialized with cert: {certPath}");
    }

    /// <summary>
    /// Configure OpenSSL's built-in session caching for TLS session resumption.
    /// 
    /// TLS 1.2: Session ID-based resumption or session tickets
    /// TLS 1.3: Pre-Shared Key (PSK) based resumption with optional 0-RTT
    /// 
    /// Benefits:
    /// - 50-80% latency reduction for resumed sessions
    /// - Significant CPU reduction (skip ECDHE key exchange)
    /// - Reduced network round trips
    /// </summary>
    private void ConfigureSessionCaching()
    {
        // Enable server-side session caching
        OpenSsl.SetSessionCacheMode(_ctx, OpenSsl.SSL_SESS_CACHE_SERVER);

        // Set session timeout to 1 hour (3600 seconds)
        // This is a reasonable balance between security and performance
        OpenSsl.SSL_CTX_set_timeout(_ctx, 3600);

        // Set cache size to 20,000 sessions
        // This accommodates high-traffic scenarios with many unique clients
        OpenSsl.SetSessionCacheSize(_ctx, 20000);

        Console.WriteLine("[SslContext] TLS session caching enabled (mode=SERVER, timeout=3600s, cache_size=20000)");
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