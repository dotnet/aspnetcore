// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class Libuv
    {
        private IntPtr _module = IntPtr.Zero;

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32")]
        public static extern bool FreeLibrary(IntPtr hModule);


        public void Load(string dllToLoad)
        {
            var module = LoadLibrary(dllToLoad);
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
            if (statusCode < 0)
            {
                throw new Exception("Status code " + statusCode);
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
            Check(_uv_loop_close(handle.DangerousGetHandle()));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_run(UvLoopHandle handle, int mode);
        uv_run _uv_run;
        public int run(UvLoopHandle handle, int mode)
        {
            return Check(_uv_run(handle, mode));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_stop(UvLoopHandle handle);
        uv_stop _uv_stop;
        public void stop(UvLoopHandle handle)
        {
            _uv_stop(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_ref(UvHandle handle);
        uv_ref _uv_ref;
        public void @ref(UvHandle handle)
        {
            _uv_ref(handle);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_unref(UvHandle handle);
        uv_unref _uv_unref;
        public void unref(UvHandle handle)
        {
            _uv_unref(handle);
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_close_cb(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void uv_close(IntPtr handle, uv_close_cb close_cb);
        uv_close _uv_close;
        public void close(UvHandle handle, uv_close_cb close_cb)
        {
            _uv_close(handle.DangerousGetHandle(), close_cb);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_async_cb(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb);
        uv_async_init _uv_async_init;
        public void async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb)
        {
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
            Check(_uv_tcp_init(loop, handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_tcp_bind(UvTcpHandle handle, ref sockaddr addr, int flags);
        uv_tcp_bind _uv_tcp_bind;
        public void tcp_bind(UvTcpHandle handle, ref sockaddr addr, int flags)
        {
            Check(_uv_tcp_bind(handle, ref addr, flags));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connection_cb(IntPtr server, int status);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_listen(UvStreamHandle handle, int backlog, uv_connection_cb cb);
        uv_listen _uv_listen;
        public void listen(UvStreamHandle handle, int backlog, uv_connection_cb cb)
        {
            Check(_uv_listen(handle, backlog, cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_accept(UvStreamHandle server, UvStreamHandle client);
        uv_accept _uv_accept;
        public void accept(UvStreamHandle server, UvStreamHandle client)
        {
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
            Check(_uv_read_start(handle, alloc_cb, read_cb));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_read_stop(UvStreamHandle handle);
        uv_read_stop _uv_read_stop;
        public void read_stop(UvStreamHandle handle)
        {
            Check(_uv_read_stop(handle));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_ip4_addr(string ip, int port, out sockaddr addr);

        uv_ip4_addr _uv_ip4_addr;
        public void ip4_addr(string ip, int port, out sockaddr addr)
        {
            Check(_uv_ip4_addr(ip, port, out addr));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int uv_ip6_addr(string ip, int port, out sockaddr addr);

        uv_ip6_addr _uv_ip6_addr;
        public void ip6_addr(string ip, int port, out sockaddr addr)
        {
            Check(_uv_ip6_addr(ip, port, out addr));
        }

        public struct sockaddr
        {
            long w;
            long x;
            long y;
            long z;
        }

        public struct uv_buf_t
        {
            public uint len;
            public IntPtr memory;
        }

    }
}