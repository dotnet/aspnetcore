// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR;

internal sealed class StreamTracker
{
    private static readonly MethodInfo _buildConverterMethod = typeof(StreamTracker).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name.Equals(nameof(BuildStream)));
    private readonly object[] _streamConverterArgs;
    private readonly ConcurrentDictionary<string, IStreamConverter> _lookup = new ConcurrentDictionary<string, IStreamConverter>();

    public StreamTracker(int streamBufferCapacity)
    {
        _streamConverterArgs = new object[] { streamBufferCapacity };
    }

    /// <summary>
    /// Creates a new stream and returns the ChannelReader for it as an object.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod",
        Justification = "BuildStream doesn't have trimming annotations.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "HubMethodDescriptor checks for ValueType streaming item types when PublishAot=true. Developers will get an exception in this situation before publishing.")]
    public object AddStream(string streamId, Type itemType, Type targetType)
    {
        Debug.Assert(RuntimeFeature.IsDynamicCodeSupported || !itemType.IsValueType, "HubMethodDescriptor ensures itemType is not a ValueType when PublishAot=true.");

        var newConverter = (IStreamConverter)_buildConverterMethod.MakeGenericMethod(itemType).Invoke(null, _streamConverterArgs)!;
        _lookup[streamId] = newConverter;
        return newConverter.GetReaderAsObject(targetType);
    }

    private bool TryGetConverter(string streamId, [NotNullWhen(true)] out IStreamConverter? converter)
    {
        if (_lookup.TryGetValue(streamId, out converter))
        {
            return true;
        }

        return false;
    }

    public bool TryProcessItem(StreamItemMessage message, [NotNullWhen(true)] out Task? task)
    {
        if (TryGetConverter(message.InvocationId!, out var converter))
        {
            task = converter.WriteToStream(message.Item);
            return true;
        }

        task = default;
        return false;
    }

    public Type GetStreamItemType(string streamId)
    {
        if (TryGetConverter(streamId, out var converter))
        {
            return converter.GetItemType();
        }

        throw new KeyNotFoundException($"No stream with id '{streamId}' could be found.");
    }

    public bool TryComplete(CompletionMessage message)
    {
        _lookup.TryRemove(message.InvocationId!, out var converter);
        if (converter == null)
        {
            return false;
        }
        converter.TryComplete(message.HasResult || message.Error == null ? null : new HubException(message.Error));
        return true;
    }

    public void CompleteAll(Exception ex)
    {
        foreach (var converter in _lookup)
        {
            converter.Value.TryComplete(ex);
        }
    }

    private static IStreamConverter BuildStream<T>(int streamBufferCapacity)
    {
        return new ChannelConverter<T>(streamBufferCapacity);
    }

    private interface IStreamConverter
    {
        Type GetItemType();
        object GetReaderAsObject(Type type);
        Task WriteToStream(object? item);
        void TryComplete(Exception? ex);
    }

    private sealed class ChannelConverter<T> : IStreamConverter
    {
        private readonly Channel<T?> _channel;

        public ChannelConverter(int streamBufferCapacity)
        {
            _channel = Channel.CreateBounded<T?>(streamBufferCapacity);
        }

        public Type GetItemType()
        {
            return typeof(T);
        }

        public object GetReaderAsObject(Type type)
        {
            if (ReflectionHelper.IsIAsyncEnumerable(type))
            {
                return _channel.Reader.ReadAllAsync();
            }
            else
            {
                return _channel.Reader;
            }
        }

        public Task WriteToStream(object? o)
        {
            return _channel.Writer.WriteAsync((T?)o).AsTask();
        }

        public void TryComplete(Exception? ex)
        {
            _channel.Writer.TryComplete(ex);
        }
    }
}
