// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class Libuv
    {
        public Libuv()
        {
            IsWindows = PlatformApis.IsWindows();
            if (!IsWindows)
            {
                IsDarwin = PlatformApis.IsDarwin();
            }
        }

        public bool IsWindows;
        public bool IsDarwin;

        public Func<string, IntPtr> LoadLibrary;
        public Func<IntPtr, bool> FreeLibrary;
        public Func<IntPtr, string, IntPtr> GetProcAddress;

        public void Load(string dllToLoad)
        {
            PlatformApis.Apply(this);

            var module = LoadLibrary(dllToLoad);

            if (module == IntPtr.Zero)
            {
                var message = "Unable to load libuv.";
                if (!IsWindows && !IsDarwin)
                {
                    // *nix box, so libuv needs to be installed
                    // TODO: fwlink?
                    message += " Make sure libuv is installed and available as libuv.so.1";
                }
                
                throw new InvalidOperationException(message);
            }

            foreach (var field in GetType().GetTypeInfo().DeclaredFields)
            {
                var procAddress = GetProcAddress(module, field.Name.TrimStart('_'));
                if (procAddress == IntPtr.Zero)
                {
                    continue;
                }
                var value = Marshal.GetDelegateForFunctionPointer(procAddress, field.FieldType);
                field.SetValue(this, value);
            }
        }

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
                error = new Exception("Error " + statusCode + " " + errorName + " " + errorDescription);
            }
            else
            {
                error = null;
            }
            return statusCode;
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_loop_init(UvLoopHandle a0);
        uv_loop_init _uv_loop_init;
        public void loop_init(UvLoopHandle handle)
        {
            Check(_uv_loop_init(handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_loop_close(IntPtr a0);
        uv_loop_close _uv_loop_close;
        public void loop_close(UvLoopHandle handle)
        {
            handle.Validate(closed: true);
            Check(_uv_loop_close(handle.InternalGetHandle()));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_run(UvLoopHandle handle, int mode);
        uv_run _uv_run;
        public int run(UvLoopHandle handle, int mode)
        {
            handle.Validate();
            return Check(_uv_run(handle, mode));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_stop(UvLoopHandle handle);
        uv_stop _uv_stop;
        public void stop(UvLoopHandle handle)
        {
            handle.Validate();
            _uv_stop(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_ref(UvHandle handle);
        uv_ref _uv_ref;
        public void @ref(UvHandle handle)
        {
            handle.Validate();
            _uv_ref(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_unref(UvHandle handle);
        uv_unref _uv_unref;
        public void unref(UvHandle handle)
        {
            handle.Validate();
            _uv_unref(handle);
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_close_cb(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_close(IntPtr handle, uv_close_cb close_cb);
        uv_close _uv_close;
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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb);
        uv_async_init _uv_async_init;
        public void async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb)
        {
            loop.Validate();
            handle.Validate();
            Check(_uv_async_init(loop, handle, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_async_send(UvAsyncHandle handle);
        uv_async_send _uv_async_send;
        public void async_send(UvAsyncHandle handle)
        {
            Check(_uv_async_send(handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_tcp_init(UvLoopHandle loop, UvTcpHandle handle);
        uv_tcp_init _uv_tcp_init;
        public void tcp_init(UvLoopHandle loop, UvTcpHandle handle)
        {
            loop.Validate();
            handle.Validate();
            Check(_uv_tcp_init(loop, handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_tcp_bind(UvTcpHandle handle, ref sockaddr addr, int flags);
        uv_tcp_bind _uv_tcp_bind;
        public void tcp_bind(UvTcpHandle handle, ref sockaddr addr, int flags)
        {
            handle.Validate();
            Check(_uv_tcp_bind(handle, ref addr, flags));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connection_cb(IntPtr server, int status);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_listen(UvStreamHandle handle, int backlog, uv_connection_cb cb);
        uv_listen _uv_listen;
        public void listen(UvStreamHandle handle, int backlog, uv_connection_cb cb)
        {
            handle.Validate();
            Check(_uv_listen(handle, backlog, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_accept(UvStreamHandle server, UvStreamHandle client);
        uv_accept _uv_accept;
        public void accept(UvStreamHandle server, UvStreamHandle client)
        {
            server.Validate();
            client.Validate();
            Check(_uv_accept(server, client));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_alloc_cb(IntPtr server, int suggested_size, out uv_buf_t buf);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_read_cb(IntPtr server, int nread, ref uv_buf_t buf);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);
        uv_read_start _uv_read_start;
        public void read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb)
        {
            handle.Validate();
            Check(_uv_read_start(handle, alloc_cb, read_cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_read_stop(UvStreamHandle handle);
        uv_read_stop _uv_read_stop;
        public void read_stop(UvStreamHandle handle)
        {
            handle.Validate();
            Check(_uv_read_stop(handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_try_write(UvStreamHandle handle, Libuv.uv_buf_t[] bufs, int nbufs);
        uv_try_write _uv_try_write;
        public int try_write(UvStreamHandle handle, Libuv.uv_buf_t[] bufs, int nbufs)
        {
            handle.Validate();
            return Check(_uv_try_write(handle, bufs, nbufs));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_write_cb(IntPtr req, int status);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate int uv_write(UvWriteReq req, UvStreamHandle handle, Libuv.uv_buf_t* bufs, int nbufs, uv_write_cb cb);
        uv_write _uv_write;
        unsafe public void write(UvWriteReq req, UvStreamHandle handle, Libuv.uv_buf_t* bufs, int nbufs, uv_write_cb cb)
        {
            req.Validate();
            handle.Validate();
            Check(_uv_write(req, handle, bufs, nbufs, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_shutdown_cb(IntPtr req, int status);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_shutdown(UvShutdownReq req, UvStreamHandle handle, uv_shutdown_cb cb);
        uv_shutdown _uv_shutdown;
        public void shutdown(UvShutdownReq req, UvStreamHandle handle, uv_shutdown_cb cb)
        {
            req.Validate();
            handle.Validate();
            Check(_uv_shutdown(req, handle, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr uv_err_name(int err);
        uv_err_name _uv_err_name;
        public unsafe String err_name(int err)
        {
            IntPtr ptr = _uv_err_name(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr uv_strerror(int err);
        uv_strerror _uv_strerror;
        public unsafe String strerror(int err)
        {
            IntPtr ptr = _uv_strerror(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_loop_size();
        uv_loop_size _uv_loop_size;
        public int loop_size()
        {
            return _uv_loop_size();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_handle_size(HandleType handleType);
        uv_handle_size _uv_handle_size;
        public int handle_size(HandleType handleType)
        {
            return _uv_handle_size(handleType);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_req_size(RequestType reqType);
        uv_req_size _uv_req_size;
        public int req_size(RequestType reqType)
        {
            return _uv_req_size(reqType);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_ip4_addr(string ip, int port, out sockaddr addr);

        uv_ip4_addr _uv_ip4_addr;
        public int ip4_addr(string ip, int port, out sockaddr addr, out Exception error)
        {
            return Check(_uv_ip4_addr(ip, port, out addr), out error);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_ip6_addr(string ip, int port, out sockaddr addr);

        uv_ip6_addr _uv_ip6_addr;
        public int ip6_addr(string ip, int port, out sockaddr addr, out Exception error)
        {
            return Check(_uv_ip6_addr(ip, port, out addr), out error);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_walk_cb(IntPtr handle, IntPtr arg);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate int uv_walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg);
        uv_walk _uv_walk;
        unsafe public void walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg)
        {
            loop.Validate();
            _uv_walk(loop, walk_cb, arg);
        }

        public uv_buf_t buf_init(IntPtr memory, int len)
        {
            return new uv_buf_t(memory, len, IsWindows);
        }

        public struct sockaddr
        {
            long x0;
            long x1;
            long x2;
            long x3;
        }

        public struct uv_buf_t
        {
            public uv_buf_t(IntPtr memory, int len, bool IsWindows)
            {
                if (IsWindows)
                {
                    x0 = (IntPtr)len;
                    x1 = memory;
                }
                else
                {
                    x0 = memory;
                    x1 = (IntPtr)len;
                }
            }

            public IntPtr x0;
            public IntPtr x1;
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
        //int handle_size_async;
        //int handle_size_tcp;
        //int req_size_write;
        //int req_size_shutdown;
    }
}