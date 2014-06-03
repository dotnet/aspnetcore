using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for Accept
    /// </summary>
    public class Listener : IDisposable
    {
        private readonly KestrelThread _thread;
        UvTcpHandle _socket;
        private readonly Action<UvStreamHandle, int, object> _connectionCallback = ConnectionCallback;

        private static void ConnectionCallback(UvStreamHandle stream, int status, object state)
        {
            ((Listener)state).OnConnection(stream, status);
        }

        public Listener(KestrelThread thread)
        {
            _thread = thread;
        }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            _thread.Post(OnStart, tcs);
            return tcs.Task;
        }

        public void OnStart(object parameter)
        {
            var tcs = (TaskCompletionSource<int>)parameter;
            try
            {
                _socket = new UvTcpHandle();
                _socket.Init(_thread.Loop);
                _socket.Bind(new IPEndPoint(IPAddress.Any, 4001));
                _socket.Listen(10, _connectionCallback, this);
                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        private void OnConnection(UvStreamHandle socket, int status)
        {
            var connection = new UvTcpHandle();
            connection.Init(_thread.Loop);
            socket.Accept(connection);
            connection.ReadStart(OnRead, null);
        }

        private void OnRead(UvStreamHandle socket, int count, byte[] data, object _)
        {
            var text = Encoding.UTF8.GetString(data);
            if (count <= 0)
            {
                socket.Close();
            }
        }

        public void Dispose()
        {
            var socket = _socket;
            _socket = null;
            _thread.Post(OnDispose, socket);
        }

        private void OnDispose(object socket)
        {
            ((UvHandle)socket).Close();
        }
    }
}
