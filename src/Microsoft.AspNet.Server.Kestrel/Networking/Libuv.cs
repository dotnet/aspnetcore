// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class Libuv
    {
        public Libuv()
        {
            IsWindows = PlatformApis.IsWindows;

            var isDarwinMono =
#if DNX451
                IsWindows ? false : PlatformApis.IsDarwin;
#else
                false;
#endif

            if (isDarwinMono)
            {
                _uv_loop_init = NativeDarwinMonoMethods.uv_loop_init;
                _uv_loop_close = NativeDarwinMonoMethods.uv_loop_close;
                _uv_run = NativeDarwinMonoMethods.uv_run;
                _uv_stop = NativeDarwinMonoMethods.uv_stop;
                _uv_ref = NativeDarwinMonoMethods.uv_ref;
                _uv_unref = NativeDarwinMonoMethods.uv_unref;
                _uv_close = NativeDarwinMonoMethods.uv_close;
                _uv_async_init = NativeDarwinMonoMethods.uv_async_init;
                _uv_async_send = NativeDarwinMonoMethods.uv_async_send;
                _uv_tcp_init = NativeDarwinMonoMethods.uv_tcp_init;
                _uv_tcp_bind = NativeDarwinMonoMethods.uv_tcp_bind;
                _uv_tcp_open = NativeDarwinMonoMethods.uv_tcp_open;
                _uv_tcp_nodelay = NativeDarwinMonoMethods.uv_tcp_nodelay;
                _uv_pipe_init = NativeDarwinMonoMethods.uv_pipe_init;
                _uv_pipe_bind = NativeDarwinMonoMethods.uv_pipe_bind;
                _uv_listen = NativeDarwinMonoMethods.uv_listen;
                _uv_accept = NativeDarwinMonoMethods.uv_accept;
                _uv_pipe_connect = NativeDarwinMonoMethods.uv_pipe_connect;
                _uv_pipe_pending_count = NativeDarwinMonoMethods.uv_pipe_pending_count;
                _uv_read_start = NativeDarwinMonoMethods.uv_read_start;
                _uv_read_stop = NativeDarwinMonoMethods.uv_read_stop;
                _uv_try_write = NativeDarwinMonoMethods.uv_try_write;
                unsafe
                {
                    _uv_write = NativeDarwinMonoMethods.uv_write;
                    _uv_write2 = NativeDarwinMonoMethods.uv_write2;
                }
                _uv_shutdown = NativeDarwinMonoMethods.uv_shutdown;
                _uv_err_name = NativeDarwinMonoMethods.uv_err_name;
                _uv_strerror = NativeDarwinMonoMethods.uv_strerror;
                _uv_loop_size = NativeDarwinMonoMethods.uv_loop_size;
                _uv_handle_size = NativeDarwinMonoMethods.uv_handle_size;
                _uv_req_size = NativeDarwinMonoMethods.uv_req_size;
                _uv_ip4_addr = NativeDarwinMonoMethods.uv_ip4_addr;
                _uv_ip6_addr = NativeDarwinMonoMethods.uv_ip6_addr;
                _uv_tcp_getpeername = NativeDarwinMonoMethods.uv_tcp_getpeername;
                _uv_tcp_getsockname = NativeDarwinMonoMethods.uv_tcp_getsockname;
                _uv_walk = NativeDarwinMonoMethods.uv_walk;
            }
            else
            {
                _uv_loop_init = NativeMethods.uv_loop_init;
                _uv_loop_close = NativeMethods.uv_loop_close;
                _uv_run = NativeMethods.uv_run;
                _uv_stop = NativeMethods.uv_stop;
                _uv_ref = NativeMethods.uv_ref;
                _uv_unref = NativeMethods.uv_unref;
                _uv_close = NativeMethods.uv_close;
                _uv_async_init = NativeMethods.uv_async_init;
                _uv_async_send = NativeMethods.uv_async_send;
                _uv_tcp_init = NativeMethods.uv_tcp_init;
                _uv_tcp_bind = NativeMethods.uv_tcp_bind;
                _uv_tcp_open = NativeMethods.uv_tcp_open;
                _uv_tcp_nodelay = NativeMethods.uv_tcp_nodelay;
                _uv_pipe_init = NativeMethods.uv_pipe_init;
                _uv_pipe_bind = NativeMethods.uv_pipe_bind;
                _uv_listen = NativeMethods.uv_listen;
                _uv_accept = NativeMethods.uv_accept;
                _uv_pipe_connect = NativeMethods.uv_pipe_connect;
                _uv_pipe_pending_count = NativeMethods.uv_pipe_pending_count;
                _uv_read_start = NativeMethods.uv_read_start;
                _uv_read_stop = NativeMethods.uv_read_stop;
                _uv_try_write = NativeMethods.uv_try_write;
                unsafe
                {
                    _uv_write = NativeMethods.uv_write;
                    _uv_write2 = NativeMethods.uv_write2;
                }
                _uv_shutdown = NativeMethods.uv_shutdown;
                _uv_err_name = NativeMethods.uv_err_name;
                _uv_strerror = NativeMethods.uv_strerror;
                _uv_loop_size = NativeMethods.uv_loop_size;
                _uv_handle_size = NativeMethods.uv_handle_size;
                _uv_req_size = NativeMethods.uv_req_size;
                _uv_ip4_addr = NativeMethods.uv_ip4_addr;
                _uv_ip6_addr = NativeMethods.uv_ip6_addr;
                _uv_tcp_getpeername = NativeMethods.uv_tcp_getpeername;
                _uv_tcp_getsockname = NativeMethods.uv_tcp_getsockname;
                _uv_walk = NativeMethods.uv_walk;
            }
        }

        public readonly bool IsWindows;

        public int Check(int statusCode)
        {
            Exception error;
            var result = Check(statusCode, out error);
            if (error != null)
            {
                throw error;
            }
            return statusCode;
        }

        public int Check(int statusCode, out Exception error)
        {
            if (statusCode < 0)
            {
                var errorName = err_name(statusCode);
                var errorDescription = strerror(statusCode);
                error = new UvException("Error " + statusCode + " " + errorName + " " + errorDescription);
            }
            else
            {
                error = null;
            }
            return statusCode;
        }

        protected Func<UvLoopHandle, int> _uv_loop_init;
        public void loop_init(UvLoopHandle handle)
        {
            Check(_uv_loop_init(handle));
        }

        protected Func<IntPtr, int> _uv_loop_close;
        public void loop_close(UvLoopHandle handle)
        {
            handle.Validate(closed: true);
            Check(_uv_loop_close(handle.InternalGetHandle()));
        }

        protected Func<UvLoopHandle, int, int> _uv_run;
        public int run(UvLoopHandle handle, int mode)
        {
            handle.Validate();
            return Check(_uv_run(handle, mode));
        }

        protected Action<UvLoopHandle> _uv_stop;
        public void stop(UvLoopHandle handle)
        {
            handle.Validate();
            _uv_stop(handle);
        }

        protected Action<UvHandle> _uv_ref;
        public void @ref(UvHandle handle)
        {
            handle.Validate();
            _uv_ref(handle);
        }

        protected Action<UvHandle> _uv_unref;
        public void unref(UvHandle handle)
        {
            handle.Validate();
            _uv_unref(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_close_cb(IntPtr handle);
        protected Action<IntPtr, uv_close_cb> _uv_close;
        public void close(UvHandle handle, uv_close_cb close_cb)
        {
            handle.Validate(closed: true);
            _uv_close(handle.InternalGetHandle(), close_cb);
        }

        public void close(IntPtr handle, uv_close_cb close_cb)
        {
            _uv_close(handle, close_cb);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_async_cb(IntPtr handle);
        protected Func<UvLoopHandle, UvAsyncHandle, uv_async_cb, int> _uv_async_init;
        public void async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb)
        {
            loop.Validate();
            handle.Validate();
            Check(_uv_async_init(loop, handle, cb));
        }

        protected Func<UvAsyncHandle, int> _uv_async_send;
        public void async_send(UvAsyncHandle handle)
        {
            Check(_uv_async_send(handle));
        }

        protected Func<UvLoopHandle, UvTcpHandle, int> _uv_tcp_init;
        public void tcp_init(UvLoopHandle loop, UvTcpHandle handle)
        {
            loop.Validate();
            handle.Validate();
            Check(_uv_tcp_init(loop, handle));
        }

        protected delegate int uv_tcp_bind_func(UvTcpHandle handle, ref SockAddr addr, int flags);
        protected uv_tcp_bind_func _uv_tcp_bind;
        public void tcp_bind(UvTcpHandle handle, ref SockAddr addr, int flags)
        {
            handle.Validate();
            Check(_uv_tcp_bind(handle, ref addr, flags));
        }

        protected Func<UvTcpHandle, IntPtr, int> _uv_tcp_open;
        public void tcp_open(UvTcpHandle handle, IntPtr hSocket)
        {
            handle.Validate();
            Check(_uv_tcp_open(handle, hSocket));
        }

        protected Func<UvTcpHandle, int, int> _uv_tcp_nodelay;
        public void tcp_nodelay(UvTcpHandle handle, bool enable)
        {
            handle.Validate();
            Check(_uv_tcp_nodelay(handle, enable ? 1 : 0));
        }

        protected Func<UvLoopHandle, UvPipeHandle, int, int> _uv_pipe_init;
        public void pipe_init(UvLoopHandle loop, UvPipeHandle handle, bool ipc)
        {
            loop.Validate();
            handle.Validate();
            Check(_uv_pipe_init(loop, handle, ipc ? -1 : 0));
        }

        protected Func<UvPipeHandle, string, int> _uv_pipe_bind;
        public void pipe_bind(UvPipeHandle handle, string name)
        {
            handle.Validate();
            Check(_uv_pipe_bind(handle, name));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connection_cb(IntPtr server, int status);
        protected Func<UvStreamHandle, int, uv_connection_cb, int> _uv_listen;
        public void listen(UvStreamHandle handle, int backlog, uv_connection_cb cb)
        {
            handle.Validate();
            Check(_uv_listen(handle, backlog, cb));
        }

        protected Func<UvStreamHandle, UvStreamHandle, int> _uv_accept;
        public void accept(UvStreamHandle server, UvStreamHandle client)
        {
            server.Validate();
            client.Validate();
            Check(_uv_accept(server, client));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connect_cb(IntPtr req, int status);
        protected Action<UvConnectRequest, UvPipeHandle, string, uv_connect_cb> _uv_pipe_connect;
        unsafe public void pipe_connect(UvConnectRequest req, UvPipeHandle handle, string name, uv_connect_cb cb)
        {
            req.Validate();
            handle.Validate();
            _uv_pipe_connect(req, handle, name, cb);
        }

        protected Func<UvPipeHandle, int> _uv_pipe_pending_count;
        unsafe public int pipe_pending_count(UvPipeHandle handle)
        {
            handle.Validate();
            return _uv_pipe_pending_count(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_alloc_cb(IntPtr server, int suggested_size, out uv_buf_t buf);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_read_cb(IntPtr server, int nread, ref uv_buf_t buf);
        protected Func<UvStreamHandle, uv_alloc_cb, uv_read_cb, int> _uv_read_start;
        public void read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb)
        {
            handle.Validate();
            Check(_uv_read_start(handle, alloc_cb, read_cb));
        }

        protected Func<UvStreamHandle, int> _uv_read_stop;
        public void read_stop(UvStreamHandle handle)
        {
            handle.Validate();
            Check(_uv_read_stop(handle));
        }

        protected Func<UvStreamHandle, uv_buf_t[], int, int> _uv_try_write;
        public int try_write(UvStreamHandle handle, uv_buf_t[] bufs, int nbufs)
        {
            handle.Validate();
            return Check(_uv_try_write(handle, bufs, nbufs));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_write_cb(IntPtr req, int status);

        unsafe protected delegate int uv_write_func(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);
        unsafe protected uv_write_func _uv_write;
        unsafe public void write(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb)
        {
            req.Validate();
            handle.Validate();
            Check(_uv_write(req, handle, bufs, nbufs, cb));
        }

        unsafe protected delegate int uv_write2_func(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb);
        unsafe protected uv_write2_func _uv_write2;
        unsafe public void write2(UvRequest req, UvStreamHandle handle, Libuv.uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb)
        {
            req.Validate();
            handle.Validate();
            Check(_uv_write2(req, handle, bufs, nbufs, sendHandle, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_shutdown_cb(IntPtr req, int status);
        protected Func<UvShutdownReq, UvStreamHandle, uv_shutdown_cb, int> _uv_shutdown;
        public void shutdown(UvShutdownReq req, UvStreamHandle handle, uv_shutdown_cb cb)
        {
            req.Validate();
            handle.Validate();
            Check(_uv_shutdown(req, handle, cb));
        }

        protected Func<int, IntPtr> _uv_err_name;
        public unsafe string err_name(int err)
        {
            IntPtr ptr = _uv_err_name(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        protected Func<int, IntPtr> _uv_strerror;
        public unsafe string strerror(int err)
        {
            IntPtr ptr = _uv_strerror(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        protected Func<int> _uv_loop_size;
        public int loop_size()
        {
            return _uv_loop_size();
        }

        protected Func<HandleType, int> _uv_handle_size;
        public int handle_size(HandleType handleType)
        {
            return _uv_handle_size(handleType);
        }

        protected Func<RequestType, int> _uv_req_size;
        public int req_size(RequestType reqType)
        {
            return _uv_req_size(reqType);
        }

        protected delegate int uv_ip4_addr_func(string ip, int port, out SockAddr addr);
        protected uv_ip4_addr_func _uv_ip4_addr;
        public int ip4_addr(string ip, int port, out SockAddr addr, out Exception error)
        {
            return Check(_uv_ip4_addr(ip, port, out addr), out error);
        }

        protected delegate int uv_ip6_addr_func(string ip, int port, out SockAddr addr);
        protected uv_ip6_addr_func _uv_ip6_addr;
        public int ip6_addr(string ip, int port, out SockAddr addr, out Exception error)
        {
            return Check(_uv_ip6_addr(ip, port, out addr), out error);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_walk_cb(IntPtr handle, IntPtr arg);
        protected Func<UvLoopHandle, uv_walk_cb, IntPtr, int> _uv_walk;
        unsafe public void walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg)
        {
            loop.Validate();
            _uv_walk(loop, walk_cb, arg);
        }

        public delegate int uv_tcp_getsockname_func(UvTcpHandle handle, out SockAddr addr, ref int namelen);
        protected uv_tcp_getsockname_func _uv_tcp_getsockname;
        public void tcp_getsockname(UvTcpHandle handle, out SockAddr addr, ref int namelen)
        {
            handle.Validate();
            Check(_uv_tcp_getsockname(handle, out addr, ref namelen));
        }

        public delegate int uv_tcp_getpeername_func(UvTcpHandle handle, out SockAddr addr, ref int namelen);
        protected uv_tcp_getpeername_func _uv_tcp_getpeername;
        public void tcp_getpeername(UvTcpHandle handle, out SockAddr addr, ref int namelen)
        {
            handle.Validate();
            Check(_uv_tcp_getpeername(handle, out addr, ref namelen));
        }

        public uv_buf_t buf_init(IntPtr memory, int len)
        {
            return new uv_buf_t(memory, len, IsWindows);
        }

        public struct uv_buf_t
        {
            // this type represents a WSABUF struct on Windows
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms741542(v=vs.85).aspx
            // and an iovec struct on *nix
            // http://man7.org/linux/man-pages/man2/readv.2.html
            // because the order of the fields in these structs is different, the field
            // names in this type don't have meaningful symbolic names. instead, they are
            // assigned in the correct order by the constructor at runtime

            private readonly IntPtr _field0;
            private readonly IntPtr _field1;

            public uv_buf_t(IntPtr memory, int len, bool IsWindows)
            {
                if (IsWindows)
                {
                    _field0 = (IntPtr)len;
                    _field1 = memory;
                }
                else
                {
                    _field0 = memory;
                    _field1 = (IntPtr)len;
                }
            }
        }

        public enum HandleType
        {
            Unknown = 0,
            ASYNC,
            CHECK,
            FS_EVENT,
            FS_POLL,
            HANDLE,
            IDLE,
            NAMED_PIPE,
            POLL,
            PREPARE,
            PROCESS,
            STREAM,
            TCP,
            TIMER,
            TTY,
            UDP,
            SIGNAL,
        }

        public enum RequestType
        {
            Unknown = 0,
            REQ,
            CONNECT,
            WRITE,
            SHUTDOWN,
            UDP_SEND,
            FS,
            WORK,
            GETADDRINFO,
            GETNAMEINFO,
        }

        private static class NativeMethods
        {
            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_init(UvLoopHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_close(IntPtr a0);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_run(UvLoopHandle handle, int mode);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_stop(UvLoopHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_ref(UvHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_unref(UvHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_close(IntPtr handle, uv_close_cb close_cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_async_send(UvAsyncHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_init(UvLoopHandle loop, UvTcpHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_bind(UvTcpHandle handle, ref SockAddr addr, int flags);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_open(UvTcpHandle handle, IntPtr hSocket);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_nodelay(UvTcpHandle handle, int enable);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_pipe_init(UvLoopHandle loop, UvPipeHandle handle, int ipc);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_pipe_bind(UvPipeHandle loop, string name);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_listen(UvStreamHandle handle, int backlog, uv_connection_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_accept(UvStreamHandle server, UvStreamHandle client);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void uv_pipe_connect(UvConnectRequest req, UvPipeHandle handle, string name, uv_connect_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_pipe_pending_count(UvPipeHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_read_stop(UvStreamHandle handle);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_try_write(UvStreamHandle handle, uv_buf_t[] bufs, int nbufs);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_write(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_write2(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_shutdown(UvShutdownReq req, UvStreamHandle handle, uv_shutdown_cb cb);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public extern static IntPtr uv_err_name(int err);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr uv_strerror(int err);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_size();

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_handle_size(HandleType handleType);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_req_size(RequestType reqType);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_ip4_addr(string ip, int port, out SockAddr addr);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_ip6_addr(string ip, int port, out SockAddr addr);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_getsockname(UvTcpHandle handle, out SockAddr name, ref int namelen);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_getpeername(UvTcpHandle handle, out SockAddr name, ref int namelen);

            [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg);
        }

        private static class NativeDarwinMonoMethods
        {
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_init(UvLoopHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_close(IntPtr a0);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_run(UvLoopHandle handle, int mode);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_stop(UvLoopHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_ref(UvHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_unref(UvHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern void uv_close(IntPtr handle, uv_close_cb close_cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_async_send(UvAsyncHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_init(UvLoopHandle loop, UvTcpHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_bind(UvTcpHandle handle, ref SockAddr addr, int flags);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_open(UvTcpHandle handle, IntPtr hSocket);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_nodelay(UvTcpHandle handle, int enable);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_pipe_init(UvLoopHandle loop, UvPipeHandle handle, int ipc);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_pipe_bind(UvPipeHandle loop, string name);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_listen(UvStreamHandle handle, int backlog, uv_connection_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_accept(UvStreamHandle server, UvStreamHandle client);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void uv_pipe_connect(UvConnectRequest req, UvPipeHandle handle, string name, uv_connect_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_pipe_pending_count(UvPipeHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public extern static int uv_read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_read_stop(UvStreamHandle handle);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_try_write(UvStreamHandle handle, uv_buf_t[] bufs, int nbufs);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_write(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_write2(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_shutdown(UvShutdownReq req, UvStreamHandle handle, uv_shutdown_cb cb);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public extern static IntPtr uv_err_name(int err);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr uv_strerror(int err);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_loop_size();

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_handle_size(HandleType handleType);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_req_size(RequestType reqType);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_ip4_addr(string ip, int port, out SockAddr addr);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_ip6_addr(string ip, int port, out SockAddr addr);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_getsockname(UvTcpHandle handle, out SockAddr name, ref int namelen);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            public static extern int uv_tcp_getpeername(UvTcpHandle handle, out SockAddr name, ref int namelen);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            unsafe public static extern int uv_walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg);
        }
    }
}