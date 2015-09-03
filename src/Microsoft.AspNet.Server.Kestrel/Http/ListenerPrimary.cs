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
    abstract public class ListenerPrimary : Listener
    {
        private List<UvPipeHandle> _dispatchPipes = new List<UvPipeHandle>();
        private int _dispatchIndex;

        // this message is passed to write2 because it must be non-zero-length, 
        // but it has no other functional significance
        private readonly ArraySegment<ArraySegment<byte>> _dummyMessage = new ArraySegment<ArraySegment<byte>>(new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) });

        protected ListenerPrimary(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        UvPipeHandle ListenPipe { get; set; }

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

        protected override void DispatchConnection(UvStreamHandle socket)
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
                    _dummyMessage,
                    socket,
                    (write2, status, error, state) => 
                    {
                        write2.Dispose();
                        ((UvStreamHandle)state).Dispose();
                    },
                    socket);
            }
        }
    }
}
