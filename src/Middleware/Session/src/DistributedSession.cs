// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    Deserialize(new MemoryStream(data));
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
                        Deserialize(new MemoryStream(data));
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

                var stream = new MemoryStream();
                Serialize(stream);

                try
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await _cache.SetAsync(
                        _sessionKey,
                        stream.ToArray(),
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
    private void Serialize(Stream output)
    {
        output.WriteByte(SerializationRevision);
        SerializeNumAs3Bytes(output, _store.Count);
        output.Write(IdBytes, 0, IdByteCount);

        foreach (var entry in _store)
        {
            var keyBytes = entry.Key.KeyBytes;
            SerializeNumAs2Bytes(output, keyBytes.Length);
            output.Write(keyBytes, 0, keyBytes.Length);
            SerializeNumAs4Bytes(output, entry.Value.Length);
            output.Write(entry.Value, 0, entry.Value.Length);
        }
    }

    private void Deserialize(Stream content)
    {
        if (content == null || content.ReadByte() != SerializationRevision)
        {
            // Replace the un-readable format.
            _isModified = true;
            return;
        }

        var expectedEntries = DeserializeNumFrom3Bytes(content);
        _sessionIdBytes = ReadBytes(content, IdByteCount);

        for (var i = 0; i < expectedEntries; i++)
        {
            var keyLength = DeserializeNumFrom2Bytes(content);
            var key = new EncodedKey(ReadBytes(content, keyLength));
            var dataLength = DeserializeNumFrom4Bytes(content);
            _store.SetValue(key, ReadBytes(content, dataLength));
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _sessionId = new Guid(_sessionIdBytes).ToString();
            _logger.SessionLoaded(_sessionKey, _sessionId, expectedEntries);
        }
    }

    private static void SerializeNumAs2Bytes(Stream output, int num)
    {
        if (num < 0 || ushort.MaxValue < num)
        {
            throw new ArgumentOutOfRangeException(nameof(num), Resources.Exception_InvalidToSerializeIn2Bytes);
        }
        output.WriteByte((byte)(num >> 8));
        output.WriteByte((byte)(0xFF & num));
    }

    private static int DeserializeNumFrom2Bytes(Stream content)
    {
        return content.ReadByte() << 8 | content.ReadByte();
    }

    private static void SerializeNumAs3Bytes(Stream output, int num)
    {
        if (num < 0 || 0xFFFFFF < num)
        {
            throw new ArgumentOutOfRangeException(nameof(num), Resources.Exception_InvalidToSerializeIn3Bytes);
        }
        output.WriteByte((byte)(num >> 16));
        output.WriteByte((byte)(0xFF & (num >> 8)));
        output.WriteByte((byte)(0xFF & num));
    }

    private static int DeserializeNumFrom3Bytes(Stream content)
    {
        return content.ReadByte() << 16 | content.ReadByte() << 8 | content.ReadByte();
    }

    private static void SerializeNumAs4Bytes(Stream output, int num)
    {
        if (num < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(num), Resources.Exception_NumberShouldNotBeNegative);
        }
        output.WriteByte((byte)(num >> 24));
        output.WriteByte((byte)(0xFF & (num >> 16)));
        output.WriteByte((byte)(0xFF & (num >> 8)));
        output.WriteByte((byte)(0xFF & num));
    }

    private static int DeserializeNumFrom4Bytes(Stream content)
    {
        return content.ReadByte() << 24 | content.ReadByte() << 16 | content.ReadByte() << 8 | content.ReadByte();
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        var output = new byte[count];
        var total = 0;
        while (total < count)
        {
            var read = stream.Read(output, total, count - total);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }
            total += read;
        }
        return output;
    }

    private sealed class DistributedSessionDebugView(DistributedSession session)
    {
        private readonly DistributedSession _session = session;

        public bool IsAvailable => _session.IsAvailable;
        public string Id => _session.Id;
        public IEnumerable<string> Keys => new List<string>(_session.Keys);
    }
}
