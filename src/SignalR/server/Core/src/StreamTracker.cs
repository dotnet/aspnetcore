// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    internal class StreamTracker
    {
        private static readonly MethodInfo _buildConverterMethod = typeof(StreamTracker).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name.Equals("BuildStream"));
        private readonly object[] _streamConverterArgs;
        private ConcurrentDictionary<string, IStreamConverter> _lookup = new ConcurrentDictionary<string, IStreamConverter>();

        public StreamTracker(int streamBufferCapacity)
        {
            _streamConverterArgs = new object[] { streamBufferCapacity };
        }

        /// <summary>
        /// Creates a new stream and returns the ChannelReader for it as an object.
        /// </summary>
        public object AddStream(string streamId, Type itemType, Type targetType)
        {
            var newConverter = (IStreamConverter)_buildConverterMethod.MakeGenericMethod(itemType).Invoke(null, _streamConverterArgs);
            _lookup[streamId] = newConverter;
            return newConverter.GetReaderAsObject(targetType);
        }

        private bool TryGetConverter(string streamId, out IStreamConverter converter)
        {
            if (_lookup.TryGetValue(streamId, out converter))
            {
                return true;
            }

            return false;
        }

        public bool TryProcessItem(StreamItemMessage message, out Task task)
        {
            if (TryGetConverter(message.InvocationId, out var converter))
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
            _lookup.TryRemove(message.InvocationId, out var converter);
            if (converter == null)
            {
                return false;
            }
            converter.TryComplete(message.HasResult || message.Error == null ? null : new Exception(message.Error));
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
            Task WriteToStream(object item);
            void TryComplete(Exception ex);
        }

        private class ChannelConverter<T> : IStreamConverter
        {
            private Channel<T> _channel;

            public ChannelConverter(int streamBufferCapacity)
            {
                _channel = Channel.CreateBounded<T>(streamBufferCapacity);
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

            public Task WriteToStream(object o)
            {
                return _channel.Writer.WriteAsync((T)o).AsTask();
            }

            public void TryComplete(Exception ex)
            {
                _channel.Writer.TryComplete(ex);
            }
        }
    }
}
