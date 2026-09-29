// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

/// <summary>
/// An <see cref="IDuplexPipe"/> implemented directly on the sans-IO TLS primitives
/// (<see cref="TlsContext"/> / <see cref="TlsBufferSession"/>), replacing the
/// SslStream-over-DuplexPipeStream layering used by <see cref="SslDuplexPipe"/>.
///
/// Kestrel otherwise runs
/// transport pipe -> DuplexPipeStream -> SslStream -> StreamPipeReader/StreamPipeWriter -> app,
/// which costs two extra buffer copies and two extra async layers in each direction.
///
/// Here the <see cref="PipeReader"/> decrypts inline, straight out of the transport pipe's
/// buffers, and the <see cref="PipeWriter"/> encrypts straight into the transport pipe's write
/// buffers. There is no Stream shim, no intermediate <see cref="Pipe"/> and no pump task.
/// </summary>
internal sealed class TlsSessionDuplexPipe : IDuplexPipe, IAsyncDisposable
{
    private const int MaxPlaintextRecord = 16 * 1024;

    // The largest record that can legally arrive on the wire, which is what the input
    // linearization below has to be able to hold. TLS 1.3 caps TLSCiphertext at 2^14 + 256, but
    // TLS 1.2 allows 2^14 + 2048, plus the 5-byte record header. Sizing this to the TLS 1.3
    // figure would silently truncate a legal TLS 1.2 record: the session would keep asking for
    // more data, the caller would keep handing it the same truncated prefix, and the connection
    // would stall until a timeout. Both values land in the same ArrayPool bucket, so the extra
    // headroom is free.
    private const int MaxCipherRecord = MaxPlaintextRecord + 2048 + 5;

    // Asking the transport for a full record forces the Pipe to allocate 32 KB segments per
    // connection. Ask for a normal segment instead and let DestinationTooSmall drain the rest.
    private const int OutputSpanHint = 4 * 1024;

    // Starting size for the per-connection plaintext/staging buffers. They grow on demand;
    // reserving a full record up front costs 32 KB per idle connection.
    private const int InitialBufferSize = 4 * 1024;

    // Below this, the tail of the current output segment is not worth emitting a record into.
    private const int MinRecordSpan = 1024;

    private const int RecordHeaderSize = 5;

    private readonly IDuplexPipe _transport;
    private readonly TlsBufferSession _session = new();
    private readonly TlsPipeReader _input;
    private readonly TlsPipeWriter _output;

    private byte[]? _scratch;

    public TlsSessionDuplexPipe(IDuplexPipe transport)
    {
        _transport = transport;
        _input = new TlsPipeReader(this);
        _output = new TlsPipeWriter(this);
    }

    public PipeReader Input => _input;

    /// <summary>Whether the reader is currently holding decrypted application data.</summary>
    internal bool HasBufferedPlaintext => _input.HasBufferedPlaintext;

    public PipeWriter Output => _output;

    public TlsBufferSession Session => _session;

    /// <summary>
    /// Drives the handshake to completion. Pass <paramref name="sniSelector"/> together with a
    /// deferred (empty-options) context to resolve the real context from the ClientHello, which
    /// is how Kestrel's SNI support would be wired up.
    ///
    /// <paramref name="onCertificateValidation"/> must record a verdict (via
    /// <c>SetRemoteCertificateValidationResult</c> or <c>AcceptWithDefaultValidation</c>).
    /// When null the peer certificate is accepted without building a chain, which is what
    /// Kestrel's <c>ClientCertificateMode.NoCertificate</c> wants; note that
    /// <c>AcceptWithDefaultValidation</c> costs a fresh <c>X509Chain</c> per connection.
    /// </summary>
    public async Task HandshakeAsync(
        TlsContext context,
        Func<SslClientHelloInfo, TlsContext>? sniSelector = null,
        Action<TlsSession>? onCertificateValidation = null,
        CancellationToken cancellationToken = default)
    {
        _session.SetContext(context);

        var result = default(ReadResult);
        var buffer = ReadOnlySequence<byte>.Empty;
        var holdsResult = false;

        // A recorded validation verdict is not reported by the call that records it. Rejecting a
        // peer certificate makes the *next* session operation throw AuthenticationException, so
        // after running validation the handshake must be driven at least once more even if the
        // session already considers the handshake complete. Without this a rejected client
        // certificate looks like a successful handshake here: the connection would be reported
        // as established, tagged with a negotiated protocol, and handed to the application, and
        // the rejection would only surface later as a read failure.
        var mustSurfaceValidationResult = false;

        try
        {
            while (!_session.IsHandshakeComplete || mustSurfaceValidationResult)
            {
                var source = holdsResult ? GetContiguous(buffer) : default;
                var destination = _transport.Output.GetSpan(OutputSpanHint);

                var status = _session.Handshake(source, destination, out var consumed, out var written);
                mustSurfaceValidationResult = false;
                _transport.Output.Advance(written);

                if (holdsResult && consumed > 0)
                {
                    buffer = buffer.Slice(consumed);
                }

                var produced = written > 0;
                produced |= DrainPendingOutput();

                if (produced)
                {
                    await _transport.Output.FlushAsync(cancellationToken);
                }

                switch (status)
                {
                    case TlsOperationStatus.Complete:
                    case TlsOperationStatus.DestinationTooSmall:
                        continue;

                    case TlsOperationStatus.NeedsTlsContext:
                        if (sniSelector is null)
                        {
                            throw new InvalidOperationException("Handshake suspended on NeedsTlsContext but no selector was supplied.");
                        }
                        _session.SetContext(sniSelector(_session.ClientHelloInfo!.Value));
                        continue;

                    case TlsOperationStatus.NeedsCertificateValidation:
                        if (onCertificateValidation is not null)
                        {
                            onCertificateValidation(_session);
                        }
                        else
                        {
                            // Accepts the peer certificate without inspecting it. Reachable only
                            // when a caller passes no validation delegate, which Kestrel never
                            // does - HttpsConnectionMiddleware always supplies
                            // AcceptWithDefaultValidation, so the configured
                            // ClientCertificateMode and ClientCertificateValidation are always
                            // applied. It exists because the session refuses to proceed until a
                            // verdict is recorded, and a caller that opted out of validation
                            // still needs the handshake to finish.
                            _session.SetRemoteCertificateValidationResult(SslPolicyErrors.None);
                        }

                        mustSurfaceValidationResult = true;
                        continue;

                    case TlsOperationStatus.NeedMoreData:
                        // The peer has closed and the session could not consume what is left, so
                        // no further input can arrive to unblock it. Without this the loop would
                        // spin: ReadAsync returns the same completed, unconsumable buffer every
                        // time, buffer.IsEmpty is false, and nothing makes progress until the
                        // handshake timeout fires - burning a core for the whole timeout on any
                        // client that sends a partial record or an alert and disconnects.
                        if (holdsResult && result.IsCompleted && consumed == 0)
                        {
                            throw new IOException("Transport closed during the TLS handshake.");
                        }

                        if (holdsResult)
                        {
                            // examined == end: park until the peer sends more.
                            _transport.Input.AdvanceTo(buffer.Start, result.Buffer.End);
                            holdsResult = false;
                        }

                        result = await _transport.Input.ReadAsync(cancellationToken);
                        buffer = result.Buffer;
                        holdsResult = true;

                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            throw new IOException("Transport closed during the TLS handshake.");
                        }
                        continue;

                    case TlsOperationStatus.Closed:
                        throw new IOException("Peer closed the connection during the TLS handshake.");

                    default:
                        throw new InvalidOperationException($"Unexpected handshake status {status}.");
                }
            }
        }
        finally
        {
            if (holdsResult)
            {
                // examined == consumed so any application bytes that arrived with the final
                // handshake flight are served immediately by the first read.
                _transport.Input.AdvanceTo(buffer.Start, buffer.Start);
            }
        }
    }

    /// <summary>
    /// Drives post-handshake client authentication (TLS 1.3) or renegotiation (TLS 1.2), the
    /// sans-IO equivalent of <c>SslStream.NegotiateClientCertificateAsync</c>.
    ///
    /// <para>The session produces a request to send, then needs the peer's reply fed back in, so
    /// this loops: ask the session for output, write and flush it, and when it reports
    /// <see cref="TlsOperationStatus.NeedMoreData"/> read the next ciphertext and hand it over.
    /// That mirrors how the socket-bound overload drives the same operation internally.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when application data is buffered or arrives mid-exchange. Renegotiation cannot be
    /// interleaved with application data, which is the same restriction SslStream imposes.
    /// </exception>
    public async Task RequestClientCertificateAsync(
        Action<TlsSession>? onCertificateValidation = null,
        CancellationToken cancellationToken = default)
    {
        if (_input.HasBufferedPlaintext)
        {
            throw new InvalidOperationException(
                "Client stream needs to be drained before renegotiation.");
        }

        // Stage the CertificateRequest (TLS 1.3) or renegotiation (TLS 1.2) and send it, then
        // drive the second handshake to completion exactly like the initial one - that is the
        // documented contract for this operation, and it is Handshake, not RequestClientCertificate,
        // that re-surfaces NeedsCertificateValidation and finally reports Complete.
        var requestStatus = _session.RequestClientCertificate(
            _transport.Output.GetSpan(MaxCipherRecord),
            out var requestWritten);

        _transport.Output.Advance(requestWritten);

        var staged = requestWritten > 0;
        staged |= DrainPendingOutput();

        if (staged)
        {
            await _transport.Output.FlushAsync(cancellationToken);
        }

        if (requestStatus == TlsOperationStatus.Closed)
        {
            throw new IOException("Peer closed the connection when a client certificate was requested.");
        }

        // Staging the request re-arms the handshake state machine, so the session must no longer
        // report the first handshake as complete. A runtime that still reports it complete has not
        // re-armed, and the Handshake loop below would short-circuit to Complete without ever
        // reading the peer's certificate - surfacing as a silent "no client certificate" rather
        // than a failure. Fail loudly instead: a caller that cannot tell those apart may treat an
        // authenticated peer as anonymous.
        if (staged && _session.IsHandshakeComplete)
        {
            throw new PlatformNotSupportedException(
                "The runtime does not support post-handshake client authentication on a sans-IO TLS session.");
        }

        {
            var result = default(ReadResult);
            var buffer = ReadOnlySequence<byte>.Empty;
            var holdsResult = false;

            try
            {
                while (true)
                {
                    var source = holdsResult ? GetContiguous(buffer) : default;
                    var destination = _transport.Output.GetSpan(OutputSpanHint);

                    var status = _session.Handshake(source, destination, out var consumed, out var written);
                    _transport.Output.Advance(written);

                    if (holdsResult && consumed > 0)
                    {
                        buffer = buffer.Slice(consumed);
                    }

                    var produced = written > 0;
                    produced |= DrainPendingOutput();

                    if (produced)
                    {
                        await _transport.Output.FlushAsync(cancellationToken);
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.Complete:
                            return;

                        case TlsOperationStatus.DestinationTooSmall:
                            continue;

                        case TlsOperationStatus.NeedsCertificateValidation:
                            if (onCertificateValidation is not null)
                            {
                                onCertificateValidation(_session);
                            }
                            else
                            {
                                _session.AcceptWithDefaultValidation();
                            }

                            continue;

                        case TlsOperationStatus.NeedMoreData:
                            // Same spin guard as the initial handshake: a completed transport
                            // holding bytes the session cannot consume will never make progress.
                            if (holdsResult && result.IsCompleted && consumed == 0)
                            {
                                throw new IOException("Transport closed during client certificate negotiation.");
                            }

                            if (holdsResult)
                            {
                                _transport.Input.AdvanceTo(buffer.Start, result.Buffer.End);
                                holdsResult = false;
                            }

                            result = await _transport.Input.ReadAsync(cancellationToken);
                            buffer = result.Buffer;
                            holdsResult = true;

                            if (buffer.IsEmpty && result.IsCompleted)
                            {
                                throw new IOException("Transport closed during client certificate negotiation.");
                            }

                            continue;

                        case TlsOperationStatus.Closed:
                            throw new IOException("Peer closed the connection during client certificate negotiation.");

                        default:
                            throw new InvalidOperationException($"Unexpected status {status} during client certificate negotiation.");
                    }
                }
            }
            finally
            {
                if (holdsResult)
                {
                    // examined == consumed so any application bytes that arrived alongside the
                    // final flight are served immediately by the next read.
                    _transport.Input.AdvanceTo(buffer.Start, buffer.Start);
                }
            }
        }
    }

    /// <summary>
    /// Picks the destination span for one TLS record.
    ///
    /// Kestrel's output pipe hands out 4 KB segments, so <c>GetSpan(OutputSpanHint)</c> can never
    /// be served from a partially used segment - it starts a fresh one on every single write.
    /// That fragments each response across segments, which turns the socket send from a
    /// single-buffer <c>sendto</c> into a vectored <c>sendmsg</c> and adds a thread hand-off per
    /// flush (measured on Linux: ~3.6x the futex traffic per request).
    ///
    /// Instead, use whatever is already left in the current segment when it is big enough to be
    /// worth a record, and only ask for a new segment when the remainder is too small. Callers
    /// pass a non-zero <paramref name="hint"/> to escalate when a short destination did not let
    /// the session make progress, which keeps the retry loops finite.
    /// </summary>
    private Span<byte> GetOutputSpan(int hint)
        // GetSpan returns the remainder of the current segment when it can satisfy the request,
        // and a fresh segment otherwise - which is exactly the "use the tail if it is worth a
        // record, else start a new segment" rule, without asking twice.
        => _transport.Output.GetSpan(hint > 0 ? hint : MinRecordSpan);

    private void Encrypt(ReadOnlySpan<byte> plaintext)
    {
        var hint = 0;

        while (!plaintext.IsEmpty)
        {
            var destination = GetOutputSpan(hint);
            var status = _session.Write(plaintext, destination, out var consumed, out var written);
            _transport.Output.Advance(written);
            plaintext = plaintext[consumed..];

            if (status == TlsOperationStatus.DestinationTooSmall)
            {
                DrainPendingOutput();

                // No progress means the space left in the segment cannot hold a record, so ask
                // for a full one next time round rather than spinning on the same short span.
                hint = consumed == 0 && written == 0 ? MaxCipherRecord : 0;
                continue;
            }

            if (consumed == 0 && written == 0)
            {
                throw new InvalidOperationException($"TLS write made no progress ({status}).");
            }

            hint = 0;
        }

        DrainPendingOutput();
    }

    private bool DrainPendingOutput()
    {
        var wrote = false;
        var hint = 0;

        while (_session.HasPendingOutput)
        {
            var destination = GetOutputSpan(hint);
            var status = _session.DrainPendingOutput(destination, out var written);
            _transport.Output.Advance(written);
            wrote |= written > 0;

            if (status != TlsOperationStatus.DestinationTooSmall)
            {
                break;
            }

            if (written == 0)
            {
                if (hint != 0)
                {
                    break;
                }

                hint = MaxCipherRecord;
            }
            else
            {
                hint = 0;
            }
        }

        return wrote;
    }

    private ReadOnlySpan<byte> GetContiguous(in ReadOnlySequence<byte> sequence)
    {
        var first = sequence.FirstSpan;

        if (sequence.IsSingleSegment)
        {
            return first;
        }

        // The session needs a whole record in one contiguous span - handed a partial one it
        // consumes nothing and asks for more, so it cannot be fed segment by segment. But it
        // does not need the *whole* backlog contiguous: it takes as many complete records as
        // the span holds and reports how much it consumed. So whenever the first segment
        // already contains a complete record, hand it over untouched and skip the copy. With
        // 4 KB transport segments and typical record sizes that is the common case; testing
        // first.Length against a full 16 KB record instead would copy almost every time.
        if (first.Length >= RecordHeaderSize)
        {
            var frameLength = RecordHeaderSize + ((first[3] << 8) | first[4]);
            if (first.Length >= frameLength)
            {
                return first;
            }
        }

        // Only a record that genuinely straddles a segment boundary needs linearizing, and
        // only one record's worth of it.
        var slice = sequence.Length > MaxCipherRecord ? sequence.Slice(0, MaxCipherRecord) : sequence;
        var length = (int)slice.Length;

        _scratch ??= ArrayPool<byte>.Shared.Rent(MaxCipherRecord);
        slice.CopyTo(_scratch);
        return _scratch.AsSpan(0, length);
    }

    /// <summary>
    /// Returns the linearization buffer used by <see cref="GetContiguous"/>.
    ///
    /// <para>That buffer exists only to bridge the transport pipe, which hands out a possibly
    /// multi-segment <see cref="ReadOnlySequence{T}"/>, to the session, which needs a whole
    /// record in one contiguous span. It holds no TLS state. It is kept between calls so a busy
    /// connection does not pay a pool round-trip per read, but an idle connection has no reason
    /// to hold 32 KB of pooled array for the rest of its life - which is exactly what the reader
    /// and writer buffers already avoid.</para>
    ///
    /// <para>Safe at the point it is called: nothing holds a span into it once the reader has no
    /// buffered plaintext, because it is only live for the duration of a single session call.</para>
    /// </summary>
    private void ReleaseScratch()
    {
        if (_scratch is not null)
        {
            ArrayPool<byte>.Shared.Return(_scratch);
            _scratch = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_session.IsHandshakeComplete)
            {
                var destination = _transport.Output.GetSpan(OutputSpanHint);
                _session.Shutdown(destination, out var written);
                _transport.Output.Advance(written);
                await _transport.Output.FlushAsync();
            }
        }
        catch
        {
            // Best effort: the peer may already be gone.
        }

        await _transport.Output.CompleteAsync();
        await _transport.Input.CompleteAsync();

        _input.ReleaseBuffers();
        _output.ReleaseBuffers();

        ReleaseScratch();

        _session.Dispose();
    }

    /// <summary>
    /// Pull-based reader: decrypts on the caller's stack during <see cref="ReadAsync"/>,
    /// so a request/response round trip needs no extra task scheduling.
    /// </summary>
    private sealed class TlsPipeReader(TlsSessionDuplexPipe owner) : PipeReader
    {
        private byte[]? _plaintext;
        private int _start;
        private int _examined;
        private int _end;
        private bool _completed;
        private bool _canceled;

        internal bool HasBufferedPlaintext => _end > _start;

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_end > _examined || _completed || _canceled)
            {
                return new ValueTask<ReadResult>(CreateResult());
            }

            return ReadSlowAsync(cancellationToken);
        }

        // Pooled builder: this runs once per read that actually goes async, so a plain
        // async ValueTask would box a state machine on every request.
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<ReadResult> ReadSlowAsync(CancellationToken cancellationToken)
        {
            while (_end <= _examined && !_completed && !_canceled)
            {
                // Ask the session first. It reports NeedMoreData when, and only when, more
                // ciphertext from the peer is actually required.
                if (TryDrainSession())
                {
                    break;
                }

                var result = await owner._transport.Input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (result.IsCanceled)
                {
                    owner._transport.Input.AdvanceTo(buffer.Start, buffer.Start);
                    _canceled = true;
                    break;
                }

                var closed = false;
                var consumedAny = false;
                while (!buffer.IsEmpty)
                {
                    var source = owner.GetContiguous(buffer);
                    EnsureCapacity(PeekPlaintextUpperBound(source));

                    var status = owner._session.Read(
                        source,
                        _plaintext.AsSpan(_end),
                        out var consumed,
                        out var produced);

                    _end += produced;
                    buffer = buffer.Slice(consumed);
                    consumedAny |= consumed > 0 || produced > 0;

                    if (status == TlsOperationStatus.Closed)
                    {
                        closed = true;
                        break;
                    }

                    if (consumed == 0)
                    {
                        // Partial record: wait for more ciphertext.
                        break;
                    }
                }

                owner._transport.Input.AdvanceTo(buffer.Start, result.Buffer.End);

                // The trailing !consumedAny case is a peer that closed mid-record. The leftover
                // bytes can never complete, so treating only the empty-buffer case as completion
                // would spin here: the session keeps asking for data the peer will never send and
                // ReadAsync keeps returning the same completed buffer.
                if (closed || (result.IsCompleted && (buffer.IsEmpty || !consumedAny)))
                {
                    _completed = true;
                }
            }

            return CreateResult();
        }

        public override bool TryRead(out ReadResult result)
        {
            if (_end > _examined || _completed || _canceled)
            {
                result = CreateResult();
                return true;
            }

            result = default;
            return false;
        }

        public override void AdvanceTo(SequencePosition consumed) => AdvanceTo(consumed, consumed);

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            var sequence = CurrentSequence();
            var consumedOffset = (int)sequence.Slice(sequence.Start, consumed).Length;
            var examinedOffset = (int)sequence.Slice(sequence.Start, examined).Length;

            _examined = _start + examinedOffset;
            _start += consumedOffset;

            if (_start >= _end)
            {
                _start = 0;
                _end = 0;
                _examined = 0;

                // Nothing buffered: hand the arrays back rather than holding up to a full
                // record for the life of a keep-alive connection. Renting again on the next
                // read costs a thread-local pool hit.
                ReleaseBuffers();
                owner.ReleaseScratch();
            }
            else if (_examined < _start)
            {
                _examined = _start;
            }
        }

        public override void CancelPendingRead()
        {
            _canceled = true;
            owner._transport.Input.CancelPendingRead();
        }

        public override void Complete(Exception? exception = null) => owner._transport.Input.Complete(exception);

        public override ValueTask CompleteAsync(Exception? exception = null) => owner._transport.Input.CompleteAsync(exception);

        internal void ReleaseBuffers()
        {
            if (_plaintext is not null)
            {
                ArrayPool<byte>.Shared.Return(_plaintext);
                _plaintext = null;
            }
        }

        /// <summary>
        /// Reads plaintext the session already holds, without touching the transport.
        ///
        /// The session - not the transport buffer - decides when more ciphertext is needed:
        /// it may be holding a whole record, a partial one, or nothing, and a record may or
        /// may not yield output. <see cref="TlsOperationStatus.NeedMoreData"/> is the only
        /// status that means "go and fetch more bytes from the peer", so it is the condition
        /// for falling through to a transport read. Stopping merely because the transport
        /// buffer was empty is what used to hang connections whose first request arrived in
        /// the same flight as the client's final handshake records.
        /// </summary>
        private bool TryDrainSession()
        {
            var drained = false;
            var escalated = false;

            while (true)
            {
                EnsureCapacity(escalated ? MaxPlaintextRecord : InitialBufferSize);

                var status = owner._session.Read(
                    ReadOnlySpan<byte>.Empty,
                    _plaintext.AsSpan(_end),
                    out _,
                    out var produced);

                _end += produced;
                drained |= produced > 0;

                switch (status)
                {
                    case TlsOperationStatus.Closed:
                        _completed = true;
                        return true;

                    case TlsOperationStatus.NeedMoreData:
                        return drained;

                    case TlsOperationStatus.DestinationTooSmall:
                        // Grow once for a full record; if that still does not fit, fall back
                        // to the transport rather than spinning here.
                        if (produced == 0)
                        {
                            if (escalated)
                            {
                                return drained;
                            }

                            escalated = true;
                        }

                        continue;

                    default:
                        // Complete: the session made progress and may hold another record.
                        if (produced == 0)
                        {
                            return drained;
                        }

                        continue;
                }
            }
        }

        private ReadOnlySequence<byte> CurrentSequence() =>
            _plaintext is null ? ReadOnlySequence<byte>.Empty : new(_plaintext.AsMemory(_start, _end - _start));

        private ReadResult CreateResult()
        {
            var canceled = _canceled;
            _canceled = false;
            return new ReadResult(CurrentSequence(), canceled, _completed);
        }

        // Decrypted output is never larger than the record's payload, so the 5-byte TLS record
        // header tells us exactly how much room this record needs.
        private static int PeekPlaintextUpperBound(ReadOnlySpan<byte> source)
        {
            if (source.Length < RecordHeaderSize)
            {
                return InitialBufferSize;
            }

            var payload = (source[3] << 8) | source[4];
            return Math.Clamp(payload, InitialBufferSize, MaxPlaintextRecord);
        }

        private void EnsureCapacity(int needed)
        {
            if (_plaintext is null)
            {
                _plaintext = ArrayPool<byte>.Shared.Rent(Math.Max(needed, InitialBufferSize));
                return;
            }

            if (_plaintext.Length - _end >= needed)
            {
                return;
            }

            if (_start > 0)
            {
                var length = _end - _start;
                _plaintext.AsSpan(_start, length).CopyTo(_plaintext);
                _examined -= _start;
                _start = 0;
                _end = length;
            }

            if (_plaintext.Length - _end < needed)
            {
                var grown = ArrayPool<byte>.Shared.Rent(Math.Max(_plaintext.Length * 2, _end + needed));
                _plaintext.AsSpan(0, _end).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_plaintext);
                _plaintext = grown;
            }
        }
    }

    /// <summary>
    /// Buffers plaintext written by the application and encrypts it straight into the
    /// transport pipe's write buffers on flush.
    /// </summary>
    private sealed class TlsPipeWriter(TlsSessionDuplexPipe owner) : PipeWriter
    {
        private byte[]? _staging;
        private int _staged;
        private long _unflushed;

        // System.Text.Json (and anything else deciding when to flush) asks the writer how
        // much is pending. PipeWriter's base implementation throws NotSupportedException,
        // which surfaced as a 500 from WriteAsJsonAsync once Kestrel's output producer
        // delegated the call down to here.
        public override bool CanGetUnflushedBytes => true;

        public override long UnflushedBytes => _unflushed;

        public override void Advance(int bytes)
        {
            _staged += bytes;
            _unflushed += bytes;

            if (_staged >= MaxPlaintextRecord)
            {
                FlushStaging();
            }
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _staging.AsMemory(_staged);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _staging.AsSpan(_staged);
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushStaging();
            _unflushed = 0;

            // The staged plaintext has been encrypted into the transport, so this array is
            // dead until the next write. Holding it would cost a buffer per idle connection.
            ReleaseBuffers();

            return owner._transport.Output.FlushAsync(cancellationToken);
        }

        public override void CancelPendingFlush() => owner._transport.Output.CancelPendingFlush();

        public override void Complete(Exception? exception = null) => owner._transport.Output.Complete(exception);

        public override ValueTask CompleteAsync(Exception? exception = null) => owner._transport.Output.CompleteAsync(exception);

        internal void ReleaseBuffers()
        {
            if (_staging is not null)
            {
                ArrayPool<byte>.Shared.Return(_staging);
                _staging = null;
            }
        }

        private void FlushStaging()
        {
            if (_staged == 0)
            {
                return;
            }

            owner.Encrypt(_staging.AsSpan(0, _staged));
            _staged = 0;
        }

        private void Ensure(int sizeHint)
        {
            if (sizeHint <= 0)
            {
                sizeHint = 1;
            }

            if (_staging is null)
            {
                _staging = ArrayPool<byte>.Shared.Rent(Math.Max(sizeHint, InitialBufferSize));
                return;
            }

            if (_staging.Length - _staged < sizeHint)
            {
                var grown = ArrayPool<byte>.Shared.Rent(Math.Max(_staging.Length * 2, _staged + sizeHint));
                _staging.AsSpan(0, _staged).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_staging);
                _staging = grown;
            }
        }
    }
}
