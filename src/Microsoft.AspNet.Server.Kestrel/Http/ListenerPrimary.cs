// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// A primary listener waits for incoming connections on a specified socket. Incoming 
    /// connections may be passed to a secondary listener to handle.
    /// </summary>
    abstract public class ListenerPrimary<T> : Listener<T>, IListenerPrimary where T : UvStreamHandle
    {
        UvPipeHandle ListenPipe { get; set; }

        List<UvPipeHandle> _dispatchPipes = new List<UvPipeHandle>();
        int _dispatchIndex;
        ArraySegment<ArraySegment<byte>> _1234 = new ArraySegment<ArraySegment<byte>>(new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) });

        protected ListenerPrimary(IMemoryPool memory) : base(memory)
        {
        }

        public async Task StartAsync(
            string pipeName,
            string scheme,
            string host,
            int port,
            KestrelThread thread,
            Func<Frame, Task> application)
        {
            await StartAsync(scheme, host, port, thread, application).ConfigureAwait(false);

            await Thread.PostAsync(_ =>
            {
                ListenPipe = new UvPipeHandle();
                ListenPipe.Init(Thread.Loop, false);
                ListenPipe.Bind(pipeName);
                ListenPipe.Listen(Constants.ListenBacklog, OnListenPipe, null);
            }, null).ConfigureAwait(false);
        }

        private void OnListenPipe(UvStreamHandle pipe, int status, Exception error, object state)
        {
            if (status < 0)
            {
                return;
            }

            var dispatchPipe = new UvPipeHandle();
            dispatchPipe.Init(Thread.Loop, true);
            try
            {
                pipe.Accept(dispatchPipe);
            }
            catch (Exception)
            {
                dispatchPipe.Dispose();
                return;
            }
            _dispatchPipes.Add(dispatchPipe);
        }

        protected override void DispatchConnection(T socket)
        {
            var index = _dispatchIndex++ % (_dispatchPipes.Count + 1);
            if (index == _dispatchPipes.Count)
            {
                base.DispatchConnection(socket);
            }
            else
            {
                var dispatchPipe = _dispatchPipes[index];
                var write = new UvWriteReq();
                write.Init(Thread.Loop);
                write.Write2(
                    dispatchPipe,
                    _1234,
                    socket,
                    (write2, status, error, state) => 
                    {
                        write2.Dispose();
                        ((T)state).Dispose();
                    },
                    socket);
            }
        }
    }
}
