// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Session;

/// <summary>
/// An <see cref="ISession"/> backed by an <see cref="IDistributedCache"/>.
/// </summary>
[DebuggerDisplay("Count = {System.Linq.Enumerable.Count(Keys)}")]
[DebuggerTypeProxy(typeof(DistributedSessionDebugView))]
public class DistributedSession : ISession
{
    private const int IdByteCount = 16;

    private const byte SerializationRevision = 2;
    private const int KeyLengthLimit = ushort.MaxValue;

    private readonly IDistributedCache _cache;
    private readonly string _sessionKey;
    private readonly TimeSpan _idleTimeout;
    private readonly TimeSpan _ioTimeout;
    private readonly Func<bool> _tryEstablishSession;
    private readonly ILogger _logger;
    private IDistributedSessionStore _store;
    private bool _isModified;
    private bool _loaded;
    private bool _isAvailable;
    private readonly bool _isNewSessionKey;
    private string? _sessionId;
    private byte[]? _sessionIdBytes;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedSession"/>.
    /// </summary>
    /// <param name="cache">The <see cref="IDistributedCache"/> used to store the session data.</param>
    /// <param name="sessionKey">A unique key used to lookup the session.</param>
    /// <param name="idleTimeout">How long the session can be inactive (e.g. not accessed) before it will expire.</param>
    /// <param name="ioTimeout">
    /// The maximum amount of time <see cref="LoadAsync(CancellationToken)"/> and <see cref="CommitAsync(CancellationToken)"/> are allowed take.
    /// </param>
    /// <param name="tryEstablishSession">
    /// A callback invoked during <see cref="Set(string, byte[])"/> to verify that modifying the session is currently valid.
    /// If the callback returns <see langword="false"/>, <see cref="Set(string, byte[])"/> throws an <see cref="InvalidOperationException"/>.
    /// <see cref="SessionMiddleware"/> provides a callback that returns <see langword="false"/> if the session was not established
    /// prior to sending the response.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="isNewSessionKey"><see langword="true"/> if establishing a new session; <see langword="false"/> if resuming a session.</param>
    public DistributedSession(
        IDistributedCache cache,
        string sessionKey,
        TimeSpan idleTimeout,
        TimeSpan ioTimeout,
        Func<bool> tryEstablishSession,
        ILoggerFactory loggerFactory,
        bool isNewSessionKey)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrEmpty(sessionKey);
        ArgumentNullException.ThrowIfNull(tryEstablishSession);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _cache = cache;
        _sessionKey = sessionKey;
        _idleTimeout = idleTimeout;
        _ioTimeout = ioTimeout;
        _tryEstablishSession = tryEstablishSession;
        // When using a NoOpSessionStore, using a dictionary as a backing store results in problematic API choices particularly with nullability.
        // We instead use a more limited contract - `IDistributedSessionStore` as the backing store that plays better.
        _store = new DefaultDistributedSessionStore();
        _logger = loggerFactory.CreateLogger<DistributedSession>();
        _isNewSessionKey = isNewSessionKey;
    }

    /// <inheritdoc />
    public bool IsAvailable
    {
        get
        {
            Load();
            return _isAvailable;
        }
    }

    /// <inheritdoc />
    public string Id
    {
        get
        {
            Load();
            if (_sessionId == null)
            {
                _sessionId = new Guid(IdBytes).ToString();
            }
            return _sessionId;
        }
    }

    private byte[] IdBytes
    {
        get
        {
            Load();
            if (_sessionIdBytes == null)
            {
                _sessionIdBytes = new byte[IdByteCount];
                RandomNumberGenerator.Fill(_sessionIdBytes);
            }
            return _sessionIdBytes;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> Keys
    {
        get
        {
            Load();
            return _store.Keys.Select(key => key.KeyString);
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
    {
        Load();
        return _store.TryGetValue(new EncodedKey(key), out value);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (IsAvailable)
        {
            var encodedKey = new EncodedKey(key);
            if (encodedKey.KeyBytes.Length > KeyLengthLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(key),
                    Resources.FormatException_KeyLengthIsExceeded(KeyLengthLimit));
            }

            if (!_tryEstablishSession())
            {
                throw new InvalidOperationException(Resources.Exception_InvalidSessionEstablishment);
            }
            _isModified = true;
            var copy = new byte[value.Length];
            Buffer.BlockCopy(src: value, srcOffset: 0, dst: copy, dstOffset: 0, count: value.Length);
            _store.SetValue(encodedKey, copy);
        }
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        Load();
        _isModified |= _store.Remove(new EncodedKey(key));
    }

    /// <inheritdoc />
    public void Clear()
    {
        Load();
        _isModified |= _store.Count > 0;
        _store.Clear();
    }

    private void Load()
    {
        if (!_loaded)
        {
            try
            {
                var data = _cache.Get(_sessionKey);
                if (data != null)
                {
                    Deserialize(data);
                }
                else if (!_isNewSessionKey)
                {
                    _logger.AccessingExpiredSession(_sessionKey);
                }
                _isAvailable = true;
            }
            catch (Exception exception)
            {
                _logger.SessionCacheReadException(_sessionKey, exception);
                _isAvailable = false;
                _sessionId = string.Empty;
                _sessionIdBytes = null;
                _store = new NoOpSessionStore();
            }
            finally
            {
                _loaded = true;
            }
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // This will throw if called directly and a failure occurs. The user is expected to handle the failures.
        if (!_loaded)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(_ioTimeout);
                try
                {
                    cts.Token.ThrowIfCancellationRequested();
                    var data = await _cache.GetAsync(_sessionKey, cts.Token);
                    if (data != null)
                    {
                        Deserialize(data);
                    }
                    else if (!_isNewSessionKey)
                    {
                        _logger.AccessingExpiredSession(_sessionKey);
                    }
                }
                catch (OperationCanceledException oex) when (!cancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
                {
                    _logger.SessionLoadingTimeout();
                    throw new OperationCanceledException("Timed out loading the session.", oex, cts.Token);
                }
            }
            _isAvailable = true;
            _loaded = true;
        }
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            cts.CancelAfter(_ioTimeout);
            if (_isModified)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    // This operation is only so we can log if the session already existed.
                    // Log and ignore failures.
                    try
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        var data = await _cache.GetAsync(_sessionKey, cts.Token);
                        if (data == null)
                        {
                            _logger.SessionStarted(_sessionKey, Id);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        _logger.SessionCacheReadException(_sessionKey, exception);
                    }
                }

                var serialized = Serialize();

                try
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await _cache.SetAsync(
                        _sessionKey,
                        serialized,
                        new DistributedCacheEntryOptions().SetSlidingExpiration(_idleTimeout),
                        cts.Token);
                    _isModified = false;
                    _logger.SessionStored(_sessionKey, Id, _store.Count);
                }
                catch (OperationCanceledException oex) when (!cancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
                {
                    _logger.SessionCommitTimeout();
                    throw new OperationCanceledException("Timed out committing the session.", oex, cts.Token);
                }
            }
            else
            {
                try
                {
                    await _cache.RefreshAsync(_sessionKey, cts.Token);
                }
                catch (OperationCanceledException oex) when (!cancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
                {
                    _logger.SessionRefreshTimeout();
                    throw new OperationCanceledException("Timed out refreshing the session.", oex, cts.Token);
                }
            }
        }
    }

    // Format:
    // Serialization revision: 1 byte, range 0-255
    // Entry count: 3 bytes, range 0-16,777,215
    // SessionId: IdByteCount bytes (16)
    // foreach entry:
    //   key name byte length: 2 bytes, range 0-65,535
    //   UTF-8 encoded key name byte[]
    //   data byte length: 4 bytes, range 0-2,147,483,647
    //   data byte[]
    private byte[] Serialize()
    {
        var count = _store.Count;
        if (count < 0 || 0xFFFFFF < count)
        {
            throw new ArgumentOutOfRangeException("num", Resources.Exception_InvalidToSerializeIn3Bytes);
        }

        var idBytes = IdBytes;

        // Compute the exact size up front so the payload is written into a single right-sized array.
        var length = 1 + 3 + IdByteCount;
        foreach (var entry in _store)
        {
            var keyLength = entry.Key.KeyBytes.Length;
            if (ushort.MaxValue < keyLength)
            {
                throw new ArgumentOutOfRangeException("num", Resources.Exception_InvalidToSerializeIn2Bytes);
            }
            length = checked(length + 2 + keyLength + 4 + entry.Value.Length);
        }

        var output = new byte[length];
        var span = output.AsSpan();

        span[0] = SerializationRevision;
        span[1] = (byte)(count >> 16);
        span[2] = (byte)(0xFF & (count >> 8));
        span[3] = (byte)(0xFF & count);
        idBytes.AsSpan(0, IdByteCount).CopyTo(span.Slice(4));
        span = span.Slice(4 + IdByteCount);

        foreach (var entry in _store)
        {
            var keyBytes = entry.Key.KeyBytes;
            BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)keyBytes.Length);
            keyBytes.CopyTo(span.Slice(2));
            span = span.Slice(2 + keyBytes.Length);

            var value = entry.Value;
            BinaryPrimitives.WriteInt32BigEndian(span, value.Length);
            value.CopyTo(span.Slice(4));
            span = span.Slice(4 + value.Length);
        }

        Debug.Assert(span.IsEmpty);
        return output;
    }

    private void Deserialize(byte[] content)
    {
        var reader = new PayloadReader(content);
        if (reader.ReadByte() != SerializationRevision)
        {
            // Replace the un-readable format.
            _isModified = true;
            return;
        }

        var expectedEntries = reader.ReadByte() << 16 | reader.ReadByte() << 8 | reader.ReadByte();
        _sessionIdBytes = reader.ReadBytes(IdByteCount);

        for (var i = 0; i < expectedEntries; i++)
        {
            var keyLength = reader.ReadByte() << 8 | reader.ReadByte();
            var key = new EncodedKey(reader.ReadBytes(keyLength));
            var dataLength = reader.ReadByte() << 24 | reader.ReadByte() << 16 | reader.ReadByte() << 8 | reader.ReadByte();
            _store.SetValue(key, reader.ReadBytes(dataLength));
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _sessionId = new Guid(_sessionIdBytes).ToString();
            _logger.SessionLoaded(_sessionKey, _sessionId, expectedEntries);
        }
    }

    // Mirrors the semantics of reading from a MemoryStream: ReadByte returns -1 past the end of the data
    // and ReadBytes throws EndOfStreamException when not enough data remains.
    private struct PayloadReader(byte[] data)
    {
        private readonly byte[] _data = data;
        private int _position;

        public int ReadByte()
        {
            var position = _position;
            if ((uint)position < (uint)_data.Length)
            {
                _position = position + 1;
                return _data[position];
            }
            return -1;
        }

        public byte[] ReadBytes(int count)
        {
            var output = new byte[count];
            var available = _data.Length - _position;
            if (available < count)
            {
                throw new EndOfStreamException();
            }
            _data.AsSpan(_position, count).CopyTo(output);
            _position += count;
            return output;
        }
    }

    private sealed class DistributedSessionDebugView(DistributedSession session)
    {
        private readonly DistributedSession _session = session;

        public bool IsAvailable => _session.IsAvailable;
        public string Id => _session.Id;
        public IEnumerable<string> Keys => new List<string>(_session.Keys);
    }
}
