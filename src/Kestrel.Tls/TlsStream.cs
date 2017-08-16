// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Tls
{
    public class TlsStream : Stream
    {
        private static unsafe OpenSsl.alpn_select_cb_t _alpnSelectCallback = AlpnSelectCallback;

        private readonly Stream _innerStream;
        private readonly byte[] _protocols;
        private readonly GCHandle _protocolsHandle;

        private IntPtr _ctx;
        private IntPtr _ssl;
        private IntPtr _inputBio;
        private IntPtr _outputBio;

        private readonly byte[] _inputBuffer = new byte[1024 * 1024];
        private readonly byte[] _outputBuffer = new byte[1024 * 1024];

        static TlsStream()
        {
            OpenSsl.SSL_library_init();
            OpenSsl.SSL_load_error_strings();
            OpenSsl.ERR_load_BIO_strings();
            OpenSsl.OpenSSL_add_all_algorithms();
        }

        public TlsStream(Stream innerStream, string certificatePath, string privateKeyPath, IEnumerable<string> protocols)
        {
            _innerStream = innerStream;
            _protocols = ToWireFormat(protocols);
            _protocolsHandle = GCHandle.Alloc(_protocols);

            _ctx = OpenSsl.SSL_CTX_new(OpenSsl.TLSv1_2_method());

            if (_ctx == IntPtr.Zero)
            {
                throw new Exception("Unable to create SSL context.");
            }

            OpenSsl.SSL_CTX_set_ecdh_auto(_ctx, 1);

            if (OpenSsl.SSL_CTX_use_certificate_file(_ctx, certificatePath, 1) != 1)
            {
                throw new Exception("Unable to load certificate file.");
            }

            if (OpenSsl.SSL_CTX_use_PrivateKey_file(_ctx, privateKeyPath, 1) != 1)
            {
                throw new Exception("Unable to load private key file.");
            }

            OpenSsl.SSL_CTX_set_alpn_select_cb(_ctx, _alpnSelectCallback, GCHandle.ToIntPtr(_protocolsHandle));

            _ssl = OpenSsl.SSL_new(_ctx);

            _inputBio = OpenSsl.BIO_new(OpenSsl.BIO_s_mem());
            OpenSsl.BIO_set_mem_eof_return(_inputBio, -1);

            _outputBio = OpenSsl.BIO_new(OpenSsl.BIO_s_mem());
            OpenSsl.BIO_set_mem_eof_return(_outputBio, -1);

            OpenSsl.SSL_set_bio(_ssl, _inputBio, _outputBio);
        }

        ~TlsStream()
        {
            if (_ssl != IntPtr.Zero)
            {
                OpenSsl.SSL_free(_ssl);
            }

            if (_ctx != IntPtr.Zero)
            {
                // This frees the BIOs.
                OpenSsl.SSL_CTX_free(_ctx);
            }

            if (_protocolsHandle.IsAllocated)
            {
                _protocolsHandle.Free();
            }
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Flush()
        {
            FlushAsync(default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            var pending = OpenSsl.BIO_ctrl_pending(_outputBio);

            while (pending > 0)
            {
                var count = OpenSsl.BIO_read(_outputBio, _outputBuffer, 0, _outputBuffer.Length);
                await _innerStream.WriteAsync(_outputBuffer, 0, count, cancellationToken);

                pending = OpenSsl.BIO_ctrl_pending(_outputBio);
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (OpenSsl.BIO_ctrl_pending(_inputBio) == 0)
            {
                var bytesRead = await _innerStream.ReadAsync(_inputBuffer, 0, _inputBuffer.Length, cancellationToken);
                OpenSsl.BIO_write(_inputBio, _inputBuffer, 0, bytesRead);
            }

            return OpenSsl.SSL_read(_ssl, buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OpenSsl.SSL_write(_ssl, buffer, offset, count);

            return FlushAsync(cancellationToken);
        }

        public async Task DoHandshakeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            OpenSsl.SSL_set_accept_state(_ssl);

            var count = 0;

            try
            {
                while ((count = await _innerStream.ReadAsync(_inputBuffer, 0, _inputBuffer.Length, cancellationToken)) > 0)
                {
                    if (count == 0)
                    {
                        throw new IOException("TLS handshake failed: the inner stream was closed.");
                    }

                    OpenSsl.BIO_write(_inputBio, _inputBuffer, 0, count);

                    var ret = OpenSsl.SSL_do_handshake(_ssl);

                    if (ret != 1)
                    {
                        var error = OpenSsl.SSL_get_error(_ssl, ret);

                        if (error != 2)
                        {
                            throw new IOException($"TLS handshake failed: {nameof(OpenSsl.SSL_do_handshake)} error {error}.");
                        }
                    }

                    await FlushAsync(cancellationToken);

                    if (ret == 1)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _protocolsHandle.Free();
            }
        }

        public string GetNegotiatedApplicationProtocol()
        {
            OpenSsl.SSL_get0_alpn_selected(_ssl, out var protocol);
            return protocol;
        }

        private static unsafe int AlpnSelectCallback(IntPtr ssl, out byte* @out, out byte outlen, byte* @in, uint inlen, IntPtr arg)
        {
            var protocols = GCHandle.FromIntPtr(arg);
            var server = (byte[])protocols.Target;

            fixed (byte* serverPtr = server)
            {
                return OpenSsl.SSL_select_next_proto(out @out, out outlen, serverPtr, (uint)server.Length, @in, (uint)inlen) == OpenSsl.OPENSSL_NPN_NEGOTIATED
                    ? OpenSsl.SSL_TLSEXT_ERR_OK
                    : OpenSsl.SSL_TLSEXT_ERR_NOACK;
            }
        }

        private static byte[] ToWireFormat(IEnumerable<string> protocols)
        {
            var buffer = new byte[protocols.Count() + protocols.Sum(protocol => protocol.Length)];

            var offset = 0;
            foreach (var protocol in protocols)
            {
                buffer[offset++] = (byte)protocol.Length;
                offset += Encoding.ASCII.GetBytes(protocol, 0, protocol.Length, buffer, offset);
            }

            return buffer;
        }
    }
}
