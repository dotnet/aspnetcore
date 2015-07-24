using System;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.KestrelTests.TestHelpers
{
    public class MockLibuv : Libuv
    {
        private UvAsyncHandle _postHandle;
        private uv_async_cb _onPost;

        private bool _stopLoop;
        private readonly ManualResetEventSlim _loopWh = new ManualResetEventSlim();

        private Func<UvStreamHandle, ArraySegment<ArraySegment<byte>>, Action<int>, int> _onWrite;

        unsafe public MockLibuv()
        {
            _uv_write = UvWrite;

            _uv_async_send = postHandle =>
            {
                _loopWh.Set();

                return 0;
            };

            _uv_async_init = (loop, postHandle, callback) =>
            {
                _postHandle = postHandle;
                _onPost = callback;

                return 0;
            };

            _uv_run = (loopHandle, mode) =>
            {
                while (!_stopLoop)
                {
                    _loopWh.Wait();
                    _loopWh.Reset();
                    _onPost(_postHandle.InternalGetHandle());
                }

                _postHandle.Dispose();
                loopHandle.Dispose();
                return 0;
            };

            _uv_stop = handle =>
            {
                _stopLoop = true;
                _loopWh.Set();
            };

            _uv_req_size = reqType => IntPtr.Size;
            _uv_loop_size = () => IntPtr.Size;
            _uv_handle_size = handleType => IntPtr.Size;
            _uv_loop_init = loop => 0;
            _uv_tcp_init = (loopHandle, tcpHandle) => 0;
            _uv_close = (handle, callback) => callback(handle);
            _uv_loop_close = handle => 0;
            _uv_unref = handle => { };
            _uv_walk = (loop, callback, ignore) => 0;
        }

        public Func<UvStreamHandle, ArraySegment<ArraySegment<byte>>, Action<int>, int> OnWrite
        {
            get
            {
                return _onWrite;
            }
            set
            {
                _onWrite = value;
            }
        }

        unsafe private int UvWrite(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb)
        {
            return _onWrite(handle, new ArraySegment<ArraySegment<byte>>(), status => cb(req.InternalGetHandle(), status));
        }
    }
}
