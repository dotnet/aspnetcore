// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

namespace AspNetCoreModule.Test.WebSocketClient
{
    public class MyTcpClient : TcpClient
    {
        public MyTcpClient(string hostname, int port) : base(hostname, port)
        {
        }

        public bool IsDead { get; set; }
        protected override void Dispose(bool disposing)
        {
            Console.WriteLine("MyClient is disposed");
            IsDead = true;
            base.Dispose(disposing);
        }
    }

    public class WebSocketConnect : IDisposable
    {
        private static int globalID;

        public WebSocketConnect()
        {
            Id = ++globalID;
            InputData = new byte[10240];            
        }
        
        public byte[] InputData { get; set; }
        public bool IsDisposed { get; set; }
        public bool Done { get; set; }


        public int Id { get; set; }

        public MyTcpClient TcpClient { get; set; }
        public Stream Stream { get; set; }

        public List<Frame> DataSent { get; set; }
        public long TotalDataSent { get; set; }
        public List<Frame> DataReceived { get; set; }
        public long TotalDataReceived { get; set; }

        override public string ToString()
        {
            return Id+"";

        }
        
        #region IDisposable Members

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Client object is disposed");

            IsDisposed = true;
            if (Stream != null)
                Stream.Close();

            if (TcpClient != null)
                TcpClient.Close();
        }

        #endregion
    }
}
