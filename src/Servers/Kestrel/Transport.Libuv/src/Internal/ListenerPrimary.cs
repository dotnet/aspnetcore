// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    /// <summary>
    /// A primary listener waits for incoming connections on a specified socket. Incoming
    /// connections may be passed to a secondary listener to handle.
    /// </summary>
    internal class ListenerPrimary : Listener
    {
        // The list of pipes that can be dispatched to (where we've confirmed the _pipeMessage)
        private readonly List<UvPipeHandle> _dispatchPipes = new List<UvPipeHandle>();
        // The list of pipes we've created but may not be part of _dispatchPipes
        private readonly List<UvPipeHandle> _createdPipes = new List<UvPipeHandle>();
        private int _dispatchIndex;
        private string _pipeName;
        private byte[] _pipeMessage;
        private IntPtr _fileCompletionInfoPtr;
        private bool _tryDetachFromIOCP = PlatformApis.IsWindows;

        // this message is passed to write2 because it must be non-zero-length,
        // but it has no other functional significance
        private readonly ArraySegment<ArraySegment<byte>> _dummyMessage = new ArraySegment<ArraySegment<byte>>(new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) });

        public ListenerPrimary(LibuvTransportContext transportContext) : base(transportContext)
        {
        }

        /// <summary>
        /// For testing purposes.
        /// </summary>
        public int UvPipeCount => _dispatchPipes.Count;

        private UvPipeHandle ListenPipe { get; set; }

        public async Task StartAsync(
            string pipeName,
            byte[] pipeMessage,
            EndPoint endPoint,
            LibuvThread thread)
        {
            _pipeName = pipeName;
            _pipeMessage = pipeMessage;

            if (_fileCompletionInfoPtr == IntPtr.Zero)
            {
                var fileCompletionInfo = new FILE_COMPLETION_INFORMATION { Key = IntPtr.Zero, Port = IntPtr.Zero };
                _fileCompletionInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fileCompletionInfo));
                Marshal.StructureToPtr(fileCompletionInfo, _fileCompletionInfoPtr, false);
            }

            await StartAsync(endPoint, thread).ConfigureAwait(false);

            await Thread.PostAsync(listener => listener.PostCallback(), this).ConfigureAwait(false);
        }

        private void PostCallback()
        {
            ListenPipe = new UvPipeHandle(Log);
            ListenPipe.Init(Thread.Loop, Thread.QueueCloseHandle, false);
            ListenPipe.Bind(_pipeName);
            ListenPipe.Listen(TransportContext.Options.Backlog,
                (pipe, status, error, state) => ((ListenerPrimary)state).OnListenPipe(pipe, status, error), this);
        }

        private void OnListenPipe(UvStreamHandle pipe, int status, UvException error)
        {
            if (status < 0)
            {
                return;
            }

            var dispatchPipe = new UvPipeHandle(Log);
            // Add to the list of created pipes for disposal tracking
            _createdPipes.Add(dispatchPipe);

            try
            {
                dispatchPipe.Init(Thread.Loop, Thread.QueueCloseHandle, true);
                pipe.Accept(dispatchPipe);

                // Ensure client sends _pipeMessage before adding pipe to _dispatchPipes.
                var readContext = new PipeReadContext(this);
                dispatchPipe.ReadStart(
                    (handle, status2, state) => ((PipeReadContext)state).AllocCallback(handle, status2),
                    (handle, status2, state) => ((PipeReadContext)state).ReadCallback(handle, status2),
                    readContext);
            }
            catch (UvException ex)
            {
                dispatchPipe.Dispose();
                Log.LogError(0, ex, "ListenerPrimary.OnListenPipe");
            }
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
                DetachFromIOCP(socket);
                var dispatchPipe = _dispatchPipes[index];
                var write = new UvWriteReq(Log);
                try
                {
                    write.Init(Thread);
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
                catch (UvException)
                {
                    write.Dispose();
                    throw;
                }
            }
        }

        private void DetachFromIOCP(UvHandle handle)
        {
            if (!_tryDetachFromIOCP)
            {
                return;
            }

            // https://msdn.microsoft.com/en-us/library/windows/hardware/ff728840(v=vs.85).aspx
            const int FileReplaceCompletionInformation = 61;
            // https://msdn.microsoft.com/en-us/library/cc704588.aspx
            const uint STATUS_INVALID_INFO_CLASS = 0xC0000003;

            var statusBlock = new IO_STATUS_BLOCK();
            var socket = IntPtr.Zero;
            Thread.Loop.Libuv.uv_fileno(handle, ref socket);

            if (NtSetInformationFile(socket, out statusBlock, _fileCompletionInfoPtr,
                (uint)Marshal.SizeOf<FILE_COMPLETION_INFORMATION>(), FileReplaceCompletionInformation) == STATUS_INVALID_INFO_CLASS)
            {
                // Replacing IOCP information is only supported on Windows 8.1 or newer
                _tryDetachFromIOCP = false;
            }
        }

        private struct IO_STATUS_BLOCK
        {
            uint status;
            ulong information;
        }

        private struct FILE_COMPLETION_INFORMATION
        {
            public IntPtr Port;
            public IntPtr Key;
        }

        [DllImport("NtDll.dll")]
        private static extern uint NtSetInformationFile(IntPtr FileHandle,
                out IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length,
                int FileInformationClass);

        public override async Task DisposeAsync()
        {
            // Call base first so the ListenSocket gets closed and doesn't
            // try to dispatch connections to closed pipes.
            await base.DisposeAsync().ConfigureAwait(false);

            if (_fileCompletionInfoPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_fileCompletionInfoPtr);
                _fileCompletionInfoPtr = IntPtr.Zero;
            }

            if (Thread.FatalError == null && ListenPipe != null)
            {
                await Thread.PostAsync(listener =>
                {
                    listener.ListenPipe.Dispose();

                    foreach (var pipe in listener._createdPipes)
                    {
                        pipe.Dispose();
                    }
                }, this).ConfigureAwait(false);
            }
        }

        private class PipeReadContext
        {
            private const int _bufferLength = 16;

            private readonly ListenerPrimary _listener;
            private readonly byte[] _buf = new byte[_bufferLength];
            private readonly IntPtr _bufPtr;
            private GCHandle _bufHandle;
            private int _bytesRead;

            public PipeReadContext(ListenerPrimary listener)
            {
                _listener = listener;
                _bufHandle = GCHandle.Alloc(_buf, GCHandleType.Pinned);
                _bufPtr = _bufHandle.AddrOfPinnedObject();
            }

            public LibuvFunctions.uv_buf_t AllocCallback(UvStreamHandle dispatchPipe, int suggestedSize)
            {
                return dispatchPipe.Libuv.buf_init(_bufPtr + _bytesRead, _bufferLength - _bytesRead);
            }

            public void ReadCallback(UvStreamHandle dispatchPipe, int status)
            {
                if (status == LibuvConstants.EOF && _bytesRead == 0)
                {
                    // This is an unexpected immediate termination of the dispatch pipe most likely caused by an
                    // external process scanning the pipe, so don't we don't log it too severely.
                    // https://github.com/dotnet/aspnetcore/issues/4741

                    dispatchPipe.Dispose();
                    _bufHandle.Free();
                    _listener.Log.LogDebug("An internal pipe was opened unexpectedly.");
                    return;
                }

                try
                {
                    dispatchPipe.Libuv.ThrowIfErrored(status);

                    _bytesRead += status;

                    if (_bytesRead == _bufferLength)
                    {
                        var correctMessage = true;

                        for (var i = 0; i < _bufferLength; i++)
                        {
                            if (_buf[i] != _listener._pipeMessage[i])
                            {
                                correctMessage = false;
                            }
                        }

                        if (correctMessage)
                        {
                            _listener._dispatchPipes.Add((UvPipeHandle)dispatchPipe);
                            dispatchPipe.ReadStop();
                            _bufHandle.Free();
                        }
                        else
                        {
                            throw new IOException("Bad data sent over an internal pipe.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    dispatchPipe.Dispose();
                    _bufHandle.Free();
                    _listener.Log.LogError(0, ex, "ListenerPrimary.ReadCallback");
                }
            }
        }
    }
}
