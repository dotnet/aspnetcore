// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public partial class TransportConnection : IHttpConnectionFeature,
                                               IConnectionIdFeature,
                                               IConnectionTransportFeature,
                                               IConnectionItemsFeature,
                                               IMemoryPoolFeature,
                                               IApplicationTransportFeature,
                                               ITransportSchedulerFeature,
                                               IConnectionLifetimeFeature,
                                               IConnectionHeartbeatFeature,
                                               IConnectionLifetimeNotificationFeature,
                                               IConnectionCompleteFeature
    {
        // NOTE: When feature interfaces are added to or removed from this TransportConnection class implementation,
        // then the list of `features` in the generated code project MUST also be updated.
        // See also: tools/CodeGenerator/TransportConnectionFeatureCollection.cs

        private Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        string IHttpConnectionFeature.ConnectionId
        {
            get => ConnectionId;
            set => ConnectionId = value;
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get => RemoteAddress;
            set => RemoteAddress = value;
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get => LocalAddress;
            set => LocalAddress = value;
        }

        int IHttpConnectionFeature.RemotePort
        {
            get => RemotePort;
            set => RemotePort = value;
        }

        int IHttpConnectionFeature.LocalPort
        {
            get => LocalPort;
            set => LocalPort = value;
        }

        MemoryPool<byte> IMemoryPoolFeature.MemoryPool => MemoryPool;

        IDuplexPipe IConnectionTransportFeature.Transport
        {
            get => Transport;
            set => Transport = value;
        }

        IDuplexPipe IApplicationTransportFeature.Application
        {
            get => Application;
            set => Application = value;
        }

        IDictionary<object, object> IConnectionItemsFeature.Items
        {
            get => Items;
            set => Items = value;
        }

        PipeScheduler ITransportSchedulerFeature.InputWriterScheduler => InputWriterScheduler;
        PipeScheduler ITransportSchedulerFeature.OutputReaderScheduler => OutputReaderScheduler;

        CancellationToken IConnectionLifetimeFeature.ConnectionClosed
        {
            get => ConnectionClosed;
            set => ConnectionClosed = value;
        }

        CancellationToken IConnectionLifetimeNotificationFeature.ConnectionClosedRequested
        {
            get => ConnectionClosedRequested;
            set => ConnectionClosedRequested = value;
        }

        void IConnectionLifetimeFeature.Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via IConnectionLifetimeFeature.Abort()."));

        void IConnectionLifetimeNotificationFeature.RequestClose() => RequestClose();

        void IConnectionHeartbeatFeature.OnHeartbeat(System.Action<object> action, object state)
        {
            OnHeartbeat(action, state);
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            if (_onCompleted == null)
            {
                _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
            }
            _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }

        public Task FireOnCompleted()
        {
            var onCompleted = _onCompleted;

            if (onCompleted == null || onCompleted.Count == 0)
            {
                return Task.CompletedTask;
            }

            return FireOnCompletedMayAwait(onCompleted);
        }

        private Task FireOnCompletedMayAwait(Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
        {
            while (onCompleted.TryPop(out var entry))
            {
                try
                {
                    var task = entry.Key.Invoke(entry.Value);
                    if (!ReferenceEquals(task, Task.CompletedTask))
                    {
                        return FireOnCompletedAwaited(task, onCompleted);
                    }
                }
                catch (Exception)
                {
                }
            }

            return Task.CompletedTask;
        }

        private async Task FireOnCompletedAwaited(Task currentTask, Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
        {
            try
            {
                await currentTask;
            }
            catch (Exception)
            {
            }

            while (onCompleted.TryPop(out var entry))
            {
                try
                {
                    await entry.Key.Invoke(entry.Value);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
