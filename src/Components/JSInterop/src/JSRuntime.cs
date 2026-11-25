// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

/// <summary>
/// Abstract base class for a JavaScript runtime.
/// </summary>
public abstract partial class JSRuntime : IJSRuntime, IDisposable
{
    internal const long WindowObjectId = 0;
    // One-way messages to JS carrying the ids of tokens, handled by the JS call dispatcher itself.
    internal const string CancellationTokenCanceledIdentifier = "__CancellationTokenCanceled";
    internal const string CancellationTokenReleasedIdentifier = "__CancellationTokenReleased";
    // One-way message to JS that .NET has read the result of a call, so JS can release the AbortSignal ids in it.
    internal const string AbortSignalResultReadIdentifier = "__AbortSignalResultRead";

    private long _nextObjectReferenceId; // Initial value of 0 signals no object, but we increment prior to assignment. The first tracked object should have id 1
    private long _nextPendingTaskId = 1; // Start at 1 because zero signals "no response needed"
    private readonly ConcurrentDictionary<long, IPendingAsyncCall> _pendingTasks = new();
    private readonly ConcurrentDictionary<long, IDotNetObjectReference> _trackedRefsById = new();
    private readonly ConcurrentDictionary<long, CancellationTokenRegistration> _cancellationRegistrations = new();
    // The tokens written by the serialization running on this thread. They are started only after the message
    // carrying their ids is sent, so that an abort can never reach JS before its id.
    [ThreadStatic] private static JSRuntime? t_serializingRuntime;
    [ThreadStatic] private static List<CancellationTokenLink>? t_serializingTokens;
    // Process-wide: a WebView page that reloads gets a new runtime but still receives messages the old one queued.
    private static long s_nextCancellationTokenId;
    // One entry per token sent to JS, keyed by id; also the lock for the index by hash code.
    private readonly Dictionary<long, OutgoingCancellationToken> _outgoingCancellationTokens = new();
    private readonly Dictionary<int, List<OutgoingCancellationToken>> _outgoingCancellationTokensByHashCode = new();
    private int _cancellationTokenSweepGCCount;
    // "released" messages queued but not sent yet; tests wait for zero.
    private int _queuedReleasedMessages;
    private int _cancellationTokensDisposed;
    // Keyed by the id the JS side gave the AbortSignal. JS sends "released" only after every call carrying the id
    // has completed and its abort was acknowledged, so an entry may be removed only then.
    private readonly ConcurrentDictionary<long, AbortSignalSource> _abortSignalSources = new();

    internal readonly ArrayBuilder<byte[]> ByteArraysToBeRevived = new();

    /// <summary>
    /// Initializes a new instance of <see cref="JSRuntime"/>.
    /// </summary>
    protected JSRuntime()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            MaxDepth = 32,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
                {
                    new DotNetObjectReferenceJsonConverterFactory(this),
                    new JSObjectReferenceJsonConverter(this),
                    new JSStreamReferenceJsonConverter(this),
                    new DotNetStreamReferenceJsonConverter(this),
                    new ByteArrayJsonConverter(this),
                    new CancellationTokenJsonConverter(this),
                }
        };
        JsonSerializerOptions.TypeInfoResolverChain.Add(JSInteropJsonTypeInfoResolver.Instance);

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            JsonSerializerOptions.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();

    /// <summary>
    /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> used to serialize and deserialize interop payloads.
    /// </summary>
    protected internal JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Gets or sets the default timeout for asynchronous JavaScript calls.
    /// </summary>
    protected TimeSpan? DefaultAsyncTimeout { get; set; }

    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// <para>
    /// <see cref="JSRuntime"/> will apply timeouts to this operation based on the value configured in <see cref="DefaultAsyncTimeout"/>. To dispatch a call with a different, or no timeout,
    /// consider using <see cref="InvokeAsync{TValue}(string, CancellationToken, object[])" />.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(WindowObjectId, identifier, JSCallType.FunctionCall, args);

    /// <summary>
    /// Invokes the specified JavaScript function asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(WindowObjectId, identifier, JSCallType.FunctionCall, cancellationToken, args);

    /// <inheritdoc />
    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args)
        => InvokeAsync<IJSObjectReference>(WindowObjectId, identifier, JSCallType.ConstructorCall, args);

    /// <inheritdoc />
    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<IJSObjectReference>(WindowObjectId, identifier, JSCallType.ConstructorCall, cancellationToken, args);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier)
        => InvokeAsync<TValue>(WindowObjectId, identifier, JSCallType.GetValue, null);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken)
        => InvokeAsync<TValue>(WindowObjectId, identifier, JSCallType.GetValue, cancellationToken, null);

    /// <inheritdoc />
    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value)
        => await InvokeAsync<IJSVoidResult>(WindowObjectId, identifier, JSCallType.SetValue, [value]);

    /// <inheritdoc />
    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value, CancellationToken cancellationToken)
        => await InvokeAsync<IJSVoidResult>(WindowObjectId, identifier, JSCallType.SetValue, cancellationToken, [value]);

    internal async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(long targetInstanceId, string identifier, JSCallType callType, object?[]? args)
    {
        if (DefaultAsyncTimeout.HasValue)
        {
            using var cts = new CancellationTokenSource(DefaultAsyncTimeout.Value);
            // We need to await here due to the using
            return await InvokeAsync<TValue>(targetInstanceId, identifier, callType, cts.Token, args);
        }

        return await InvokeAsync<TValue>(targetInstanceId, identifier, callType, CancellationToken.None, args);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to ensure JS interop arguments are linker friendly.")]
    internal ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(
        long targetInstanceId,
        string identifier,
        JSCallType callType,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        var taskId = Interlocked.Increment(ref _nextPendingTaskId);
        var pendingCall = new PendingAsyncCall<TValue>();
        if (cancellationToken.CanBeCanceled)
        {
            _cancellationRegistrations[taskId] = cancellationToken.Register(() =>
            {
                pendingCall.Cancel(cancellationToken);
                CleanupTasksAndRegistrations(taskId);
            });
        }
        _pendingTasks[taskId] = pendingCall;

        List<CancellationTokenLink>? argumentTokens = null;
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                pendingCall.Cancel(cancellationToken);
                CleanupTasksAndRegistrations(taskId);

                return new ValueTask<TValue>(pendingCall.Task);
            }

            var argsJson = args is not null && args.Length != 0 ? SerializeForJS(args, out argumentTokens) : "[]";
            var resultType = JSCallResultTypeHelper.FromGeneric<TValue>();
            var invocationInfo = new JSInvocationInfo
            {
                AsyncHandle = taskId,
                TargetInstanceId = targetInstanceId,
                Identifier = identifier,
                CallType = callType,
                ResultType = resultType,
                ArgsJson = argsJson,
            };

            BeginInvokeJS(invocationInfo);
            LinkCancellationTokens(argumentTokens);

            return new ValueTask<TValue>(pendingCall.Task);
        }
        catch
        {
            CleanupTasksAndRegistrations(taskId);
            throw;
        }
    }

    private void CleanupTasksAndRegistrations(long taskId)
    {
        _pendingTasks.TryRemove(taskId, out _);
        if (_cancellationRegistrations.TryRemove(taskId, out var registration))
        {
            registration.Dispose();
        }
    }

    // Serializes a call or a result for JS. Pass the tokens to LinkCancellationTokens once the message is sent.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Interop arguments are required to be linker friendly.")]
    internal string SerializeForJS<T>(T value, out List<CancellationTokenLink>? cancellationTokens)
    {
        var previousRuntime = t_serializingRuntime;
        var previousTokens = t_serializingTokens;
        t_serializingRuntime = this;
        t_serializingTokens = null;
        try
        {
            var json = JsonSerializer.Serialize(value, JsonSerializerOptions);
            cancellationTokens = t_serializingTokens;
            return json;
        }
        finally
        {
            t_serializingRuntime = previousRuntime;
            t_serializingTokens = previousTokens;
        }
    }

    internal long AddSerializingCancellationToken(CancellationToken token)
    {
        if (!ReferenceEquals(t_serializingRuntime, this))
        {
            throw new InvalidOperationException($"A {nameof(CancellationToken)} can only be sent to JavaScript in the arguments of an interop call or in the result of a .NET method.");
        }

        ObjectDisposedException.ThrowIf(Volatile.Read(ref _cancellationTokensDisposed) != 0, this);

        var serializingTokens = t_serializingTokens ??= [];
        foreach (var serializing in serializingTokens)
        {
            if (serializing.Token.Equals(token))
            {
                return serializing.Id;
            }
        }

        // The list holds the link strongly until the message is sent, so a sweep cannot release a reused id meanwhile.
        CancellationTokenLink link;
        do
        {
            link = GetOrAddOutgoingCancellationToken(token);
        }
        while (!link.TryReuse());

        serializingTokens.Add(link);
        return link.Id;
    }

    private CancellationTokenLink GetOrAddOutgoingCancellationToken(CancellationToken token)
    {
        var hashCode = token.GetHashCode();
        lock (_outgoingCancellationTokens)
        {
            if (_outgoingCancellationTokensByHashCode.TryGetValue(hashCode, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Link.TryGetTarget(out var existing) && !existing.IsStale && existing.Token.Equals(token))
                    {
                        return existing;
                    }
                }
            }
            else
            {
                candidates = [];
                _outgoingCancellationTokensByHashCode.Add(hashCode, candidates);
            }

            var link = new CancellationTokenLink(this, Interlocked.Increment(ref s_nextCancellationTokenId), token);
            var entry = new OutgoingCancellationToken(link, hashCode);
            candidates.Add(entry);
            _outgoingCancellationTokens.Add(link.Id, entry);
            return link;
        }
    }

    // Must be called with the lock held.
    private void RemoveOutgoingCancellationToken(OutgoingCancellationToken entry)
    {
        _outgoingCancellationTokens.Remove(entry.Id);
        var candidates = _outgoingCancellationTokensByHashCode[entry.HashCode];
        candidates.Remove(entry);
        if (candidates.Count == 0)
        {
            _outgoingCancellationTokensByHashCode.Remove(entry.HashCode);
        }
    }

    // Every message that carried an id starts its link, so a reused id is linked even if its first message failed.
    internal void LinkCancellationTokens(List<CancellationTokenLink>? cancellationTokens)
    {
        if (cancellationTokens is not null)
        {
            foreach (var link in cancellationTokens)
            {
                link.Start();
            }
        }

        ReleaseCollectedCancellationTokens();
    }

    // A link can only be collected by a garbage collection, so the links are checked at most once after each one.
    // Messages hold their links until sent, so a collected link's messages are already with the host, ahead of "released".
    private void ReleaseCollectedCancellationTokens()
    {
        // Mono may count a full collection only in the highest generation.
        var gcCount = GC.CollectionCount(0) + GC.CollectionCount(GC.MaxGeneration);
        if (Interlocked.Exchange(ref _cancellationTokenSweepGCCount, gcCount) == gcCount)
        {
            return;
        }

        List<OutgoingCancellationToken>? collected = null;
        lock (_outgoingCancellationTokens)
        {
            foreach (var entry in _outgoingCancellationTokens.Values)
            {
                if (!entry.Link.TryGetTarget(out _))
                {
                    (collected ??= []).Add(entry);
                }
            }

            if (collected is null)
            {
                return;
            }

            foreach (var entry in collected)
            {
                RemoveOutgoingCancellationToken(entry);
            }
        }

        var released = new List<long>(collected.Count);
        foreach (var entry in collected)
        {
            released.Add(entry.Id);
        }

        QueueReleasedMessage(released);
    }

    // Never inline: WPF and WinForms WebView hosts send at once on the UI thread but queue sends from other threads,
    // so an inline "released" could overtake a queued result or cancel carrying the id. A late "released" is harmless.
    private void QueueReleasedMessage(List<long> ids)
    {
        Interlocked.Increment(ref _queuedReleasedMessages);
        ThreadPool.UnsafeQueueUserWorkItem(static state =>
        {
            try
            {
                state.Runtime.SendCancellationTokenMessage(CancellationTokenReleasedIdentifier, state.Ids);
            }
            finally
            {
                Interlocked.Decrement(ref state.Runtime._queuedReleasedMessages);
            }
        }, (Runtime: this, Ids: ids), preferLocal: false);
    }

    // Swallows failures: they would otherwise surface from the caller's Cancel() or from an unrelated call.
    private void SendCancellationTokenMessage(string identifier, List<long> ids)
    {
        try
        {
            BeginInvokeJS(new JSInvocationInfo
            {
                AsyncHandle = 0,
                TargetInstanceId = WindowObjectId,
                Identifier = identifier,
                CallType = JSCallType.FunctionCall,
                ResultType = JSCallResultType.JSVoidResult,
                ArgsJson = $"[{string.Join(',', ids)}]",
            });
        }
        catch
        {
        }
    }

    /// <summary>
    /// Begins an asynchronous function invocation.
    /// </summary>
    /// <param name="taskId">The identifier for the function invocation, or zero if no async callback is required.</param>
    /// <param name="identifier">The identifier for the function to invoke.</param>
    /// <param name="argsJson">A JSON representation of the arguments.</param>
    protected virtual void BeginInvokeJS(long taskId, string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson)
        => BeginInvokeJS(taskId, identifier, argsJson, JSCallResultType.Default, WindowObjectId);

    /// <summary>
    /// Begins an asynchronous function invocation with the call type <see cref="JSCallType.FunctionCall"/>.
    /// For more configuration options, use the overload <see cref="BeginInvokeJS(in JSInvocationInfo)" />.
    /// </summary>
    /// <param name="taskId">The identifier for the function invocation, or zero if no async callback is required.</param>
    /// <param name="identifier">The identifier for the function to invoke.</param>
    /// <param name="argsJson">A JSON representation of the arguments.</param>
    /// <param name="resultType">The type of result expected from the invocation.</param>
    /// <param name="targetInstanceId">The instance ID of the target JS object.</param>
    protected abstract void BeginInvokeJS(long taskId, string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson, JSCallResultType resultType, long targetInstanceId);

    /// <summary>
    /// Begins an asynchronous function invocation.
    /// </summary>
    /// <param name="invocationInfo">Configuration of the interop call from .NET to JavaScript.</param>
    protected virtual void BeginInvokeJS(in JSInvocationInfo invocationInfo)
    {
        BeginInvokeJS(invocationInfo.AsyncHandle, invocationInfo.Identifier, invocationInfo.ArgsJson, invocationInfo.ResultType, invocationInfo.TargetInstanceId);
    }

    /// <summary>
    /// Completes an async JS interop call from JavaScript to .NET
    /// </summary>
    /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
    /// <param name="invocationResult">The <see cref="DotNetInvocationResult"/>.</param>
    protected internal abstract void EndInvokeDotNet(
        DotNetInvocationInfo invocationInfo,
        in DotNetInvocationResult invocationResult);

    /// <summary>
    /// Transfers a byte array from .NET to JS.
    /// </summary>
    /// <param name="id">Atomically incrementing identifier for the byte array being transferred.</param>
    /// <param name="data">Byte array to be transferred to JS.</param>
    protected internal virtual void SendByteArray(int id, byte[] data)
    {
        throw new NotSupportedException("JSRuntime subclasses are responsible for implementing byte array transfer to JS.");
    }

    /// <summary>
    /// Accepts the byte array data being transferred from JS to DotNet.
    /// </summary>
    /// <param name="id">Identifier for the byte array being transferred.</param>
    /// <param name="data">Byte array to be transferred from JS.</param>
    protected internal virtual void ReceiveByteArray(int id, byte[] data)
    {
        if (id == 0)
        {
            // Starting a new transfer, clear out previously stored byte arrays
            // in case they haven't been cleared already.
            ByteArraysToBeRevived.Clear();
        }

        if (id != ByteArraysToBeRevived.Count)
        {
            throw new ArgumentOutOfRangeException($"Element id '{id}' cannot be added to the byte arrays to be revived with length '{ByteArraysToBeRevived.Count}'.", innerException: null);
        }

        ByteArraysToBeRevived.Append(data);
    }

    /// <summary>
    /// Provides a <see cref="Stream"/> for the data reference represented by <paramref name="jsStreamReference"/>.
    /// </summary>
    /// <param name="jsStreamReference"><see cref="IJSStreamReference"/> to produce a data stream for.</param>
    /// <param name="totalLength">Expected length of the incoming data stream.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken" /> for cancelling read.</param>
    /// <returns><see cref="Stream"/> for the data reference represented by <paramref name="jsStreamReference"/>.</returns>
    protected internal virtual Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
    {
        // The reason it's virtual and not abstract is just for back-compat

        // JSRuntime subclasses should override this method, and implement their own system for returning a Stream
        // representing the contents of the IJSObjectReference (whose value on the JS side will be an ArrayBufferLike).
        // The transport mechanism will be completely different between, say, Server and WebAssembly.
        throw new NotSupportedException("The current JavaScript runtime does not support reading data streams.");
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:RequiresUnreferencedCode", Justification = "We enforce trimmer attributes for JSON deserialized types on InvokeAsync.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We enforce trimmer attributes for JSON deserialized types on InvokeAsync.")]
    internal bool EndInvokeJS(long taskId, bool succeeded, ref Utf8JsonReader jsonReader)
    {
        if (!_pendingTasks.TryRemove(taskId, out var pendingCall))
        {
            // We should simply return if we can't find an id for the invocation.
            // This likely means that the method that initiated the call defined a timeout and stopped waiting.
            return false;
        }

        CleanupTasksAndRegistrations(taskId);

        try
        {
            if (succeeded)
            {
                pendingCall.Complete(this, ref jsonReader);
            }
            else
            {
                var exceptionText = jsonReader.GetString() ?? string.Empty;
                pendingCall.Fail(new JSException(exceptionText));
            }

            return true;
        }
        catch (Exception exception)
        {
            var message = $"An exception occurred executing JS interop: {exception.Message}. See InnerException for more details.";
            pendingCall.Fail(new JSException(message, exception));
            return false;
        }
    }

    internal void SendAbortSignalResultRead(long taskId)
        => SendCancellationTokenMessage(AbortSignalResultReadIdentifier, [taskId]);

    /// <summary>
    /// Transmits the stream data from .NET to JS. Subclasses should override this method and provide
    /// an implementation that transports the data to JS and calls DotNet.jsCallDispatcher.supplyDotNetStream.
    /// </summary>
    /// <param name="streamId">An identifier for the stream.</param>
    /// <param name="dotNetStreamReference">Reference to the .NET stream along with whether the stream should be left open.</param>
    protected internal virtual Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        if (!dotNetStreamReference.LeaveOpen)
        {
            dotNetStreamReference.Stream.Dispose();
        }

        throw new NotSupportedException("The current JS runtime does not support sending streams from .NET to JS.");
    }

    internal long BeginTransmittingStream(DotNetStreamReference dotNetStreamReference)
    {
        // It's fine to share the ID sequence
        var streamId = Interlocked.Increment(ref _nextObjectReferenceId);

        // Fire and forget sending the stream so the client can proceed to
        // reading the stream.
        _ = TransmitStreamAsync(streamId, dotNetStreamReference);

        return streamId;
    }

    internal long TrackObjectReference<[DynamicallyAccessedMembers(JSInvokable)] TValue>(DotNetObjectReference<TValue> dotNetObjectReference) where TValue : class
    {
        ArgumentNullException.ThrowIfNull(dotNetObjectReference);

        dotNetObjectReference.ThrowIfDisposed();

        var jsRuntime = dotNetObjectReference.JSRuntime;
        if (jsRuntime is null)
        {
            var dotNetObjectId = Interlocked.Increment(ref _nextObjectReferenceId);

            dotNetObjectReference.JSRuntime = this;
            dotNetObjectReference.ObjectId = dotNetObjectId;

            _trackedRefsById[dotNetObjectId] = dotNetObjectReference;
        }
        else if (!ReferenceEquals(this, jsRuntime))
        {
            throw new InvalidOperationException($"{dotNetObjectReference.GetType().Name} is already being tracked by a different instance of {nameof(JSRuntime)}." +
                $" A common cause is caching an instance of {nameof(DotNetObjectReference<TValue>)} globally. Consider creating instances of {nameof(DotNetObjectReference<TValue>)} at the JSInterop callsite.");
        }

        Debug.Assert(dotNetObjectReference.ObjectId != 0);
        return dotNetObjectReference.ObjectId;
    }

    internal IDotNetObjectReference GetObjectReference(long dotNetObjectId)
    {
        return _trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef)
            ? dotNetObjectRef
            : throw new ArgumentException($"There is no tracked object with id '{dotNetObjectId}'. Perhaps the DotNetObjectReference instance was already disposed.", nameof(dotNetObjectId));
    }

    /// <summary>
    /// Stops tracking the specified .NET object reference.
    /// This may be invoked either by disposing a DotNetObjectRef in .NET code, or via JS interop by calling "dispose" on the corresponding instance in JavaScript code
    /// </summary>
    /// <param name="dotNetObjectId">The ID of the <see cref="DotNetObjectReference{TValue}"/>.</param>
    internal void ReleaseObjectReference(long dotNetObjectId)
    {
        _trackedRefsById.TryRemove(dotNetObjectId, out _);
    }

    internal CancellationToken GetAbortSignalToken(long abortSignalId, bool aborted)
    {
        var source = GetAbortSignalSource(abortSignalId);
        if (aborted)
        {
            // Left unobserved: with no logger here, TaskScheduler.UnobservedTaskException is the only report of a failing callback.
            _ = source.Cancel();
        }

        return source.Token;
    }

    // Kept after cancellation: a call carrying the id may still arrive after the abort.
    internal Task AbortSignalAborted(long abortSignalId)
        => GetAbortSignalSource(abortSignalId).Cancel();

    // The source is not disposed, because application code may still hold its token.
    internal void AbortSignalReleased(long abortSignalId)
        => _abortSignalSources.TryRemove(abortSignalId, out _);

    private AbortSignalSource GetAbortSignalSource(long abortSignalId)
        => _abortSignalSources.GetOrAdd(abortSignalId, static _ => new AbortSignalSource());

    /// <summary>
    /// Dispose the JSRuntime.
    /// </summary>
    public void Dispose()
    {
        // Unregistered, so that a long-lived CancellationTokenSource does not keep this runtime reachable.
        Interlocked.Exchange(ref _cancellationTokensDisposed, 1);
        List<CancellationTokenLink> links = [];
        lock (_outgoingCancellationTokens)
        {
            foreach (var entry in _outgoingCancellationTokens.Values)
            {
                if (entry.Link.TryGetTarget(out var link))
                {
                    links.Add(link);
                }
            }

            _outgoingCancellationTokens.Clear();
            _outgoingCancellationTokensByHashCode.Clear();
        }

        // Outside the table lock: a link's lock may be held by a registration that sends a cancel.
        foreach (var link in links)
        {
            link.Stop();
        }

        _abortSignalSources.Clear();
        ByteArraysToBeRevived.Dispose();
    }

    // The id stays until the link is collected: a call carrying it may still reach JS after the cancel, and JS
    // keeps giving it the aborted signal until "released". The source drops the link's registration once canceled.
    private void OnCancellationTokenCanceled(long id)
        => SendCancellationTokenMessage(CancellationTokenCanceledIdentifier, [id]);

    // Once started, referenced strongly only by the token's registration, so it lives as long as the token can be canceled.
    internal sealed class CancellationTokenLink(JSRuntime runtime, long id, CancellationToken token)
    {
        private readonly Lock _lock = new();
        private CancellationTokenRegistration _registration;
        private bool _started;
        private volatile bool _isStale;

        public long Id { get; } = id;

        public CancellationToken Token { get; } = token;

        // Set when the source was reset, so the token needs a new id.
        public bool IsStale => _isStale;

        public void Start()
        {
            lock (_lock)
            {
                if (!_started)
                {
                    _started = true;
                    Register();
                }
            }
        }

        // Called before a message reuses the id. Unregister() is false once TryReset() removed the registration, since the
        // source never reuses a registration id, or once the callback ran. Either way the token needs a new id: a canceled
        // token cannot tell the two apart, and the new link sends its own cancel when registered.
        public bool TryReuse()
        {
            lock (_lock)
            {
                if (!_started)
                {
                    return true;
                }

                if (_registration.Unregister())
                {
                    Register();
                    return true;
                }

                _isStale = true;
                return false;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _registration.Unregister();
            }
        }

        // Must be called with the lock held.
        private void Register()
        {
            _registration = Token.UnsafeRegister(static state => ((CancellationTokenLink)state!).OnCanceled(), this);
            if (Volatile.Read(ref runtime._cancellationTokensDisposed) != 0)
            {
                _registration.Unregister();
            }
        }

        private void OnCanceled()
        {
            runtime.OnCancellationTokenCanceled(Id);

            // The source drops the registration before the callback, so keep the link reachable until the cancel is sent:
            // otherwise a sweep could queue "released" ahead of it.
            GC.KeepAlive(this);
        }
    }

    private sealed class OutgoingCancellationToken(CancellationTokenLink link, int hashCode)
    {
        public long Id { get; } = link.Id;

        public int HashCode { get; } = hashCode;

        // Weak, because only the token's registration may keep a link alive: a disposed or collected source releases its id.
        // Tracks resurrection, so a source that a finalizer still cancels is not released first.
        public WeakReference<CancellationTokenLink> Link { get; } = new(link, trackResurrection: true);
    }

    private sealed class AbortSignalSource
    {
        private readonly CancellationTokenSource _source = new();
        private Task? _cancellation;

        public CancellationToken Token => _source.Token;

        // One cancellation shared by the "aborted" state in an argument and the abort message,
        // so callback failures still reach the abort message's acknowledgment.
        public Task Cancel()
        {
            lock (_source)
            {
                return _cancellation ??= _source.CancelAsync();
            }
        }
    }
}
