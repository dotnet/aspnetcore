// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace AspNetCoreModule.Test.WebSocketClient
{
    public class WebSocketClientHelper : IDisposable
    {
        public bool IsOpened { get; private set; }
        public WebSocketConnect Connection { get; set; }
        public bool StoreData { get; set; }
        public bool IsAlwaysReading { get; private set; }
        public Uri Address { get; set; }
        public byte[][] HandShakeRequest { get; set; }
        public WebSocketState WebSocketState { get; set; }
                
        public WebSocketClientHelper()
        {
        }

        public void Dispose()
        {
            if (IsOpened)
            {
                Close();
            }
        }

        public bool WaitForWebSocketState(WebSocketState expectedState, int timeout = 3000)
        {
            bool result = false;
            int RETRYMAX = 300;
            int INTERVAL = 100; // ms
            if (timeout > RETRYMAX * INTERVAL)
            {
                throw new Exception("timeout should be less than " + 100 * 300);
            }
            for (int i=0; i<RETRYMAX; i++)
            {
                if (i*100 > timeout)
                {
                    break;
                }
                if (this.WebSocketState == expectedState)
                {
                    result = true;
                    break;
                }
                else
                {
                    Thread.Sleep(INTERVAL);
                }
            }
            return result;
        }

        public Frame Connect(Uri address, bool storeData, bool isAlwaysReading, bool waitForConnectionOpen = true)
        {
            Address = address;
            StoreData = storeData;

            Connection = new WebSocketConnect();
            if (isAlwaysReading)
            {
                InitiateWithAlwaysReading();
            }
            SendWebSocketRequest(WebSocketClientUtility.WebSocketVersion);

            if (waitForConnectionOpen)
            {
                if (!WaitForWebSocketState(WebSocketState.ConnectionOpen))
                {
                    throw new Exception("Failed to open a connection");
                }
            }
            else
            {
                Thread.Sleep(3000);
            }

            if (this.WebSocketState == WebSocketState.ConnectionOpen)
            {
                IsOpened = true;
            }
            else
            {
                IsOpened = false;
            }

            Frame openingFrame = null;

            if (!IsAlwaysReading)
                openingFrame = ReadData();
            else
                openingFrame = Connection.DataReceived[0];
            
            return openingFrame;
        }
        
        public Frame Close()
        {
            CloseConnection();
            
            Frame closeFrame = null;

            if (!IsAlwaysReading)
                closeFrame = ReadData();
            else
                closeFrame = Connection.DataReceived[Connection.DataReceived.Count - 1];

            IsOpened = false;
            return closeFrame;
        }

        public void Initiate()
        {
            string host = Address.DnsSafeHost;
            int port = Address.Port;

            Connection = new WebSocketConnect();
            TestUtility.LogInformation("Connecting to {0} on {1}", host, port);

            Connection.TcpClient = new MyTcpClient(host, port);
            Connection.Stream = Connection.TcpClient.GetStream();
            IsAlwaysReading = false;

            if (StoreData)
            {
                Connection.DataSent = new List<Frame>();
                Connection.DataReceived = new List<Frame>();
            }
        }

        public void InitiateWithAlwaysReading()
        {
            Initiate();
            Connection.Stream.BeginRead(Connection.InputData, 0, Connection.InputData.Length, ReadDataCallback, Connection);
            IsAlwaysReading = true;
        }

        public void SendWebSocketRequest(int websocketVersion)
        {
            HandShakeRequest = Frames.GetHandShakeFrame(Address.AbsoluteUri, websocketVersion);
           
            byte[] outputData = null;
            int offset = 0;
            while (offset < HandShakeRequest.Length)
            {
                outputData = HandShakeRequest[offset++];

                var result = Connection.Stream.BeginWrite(outputData, 0, outputData.Length, WriteCallback, Connection);
                
                //jhkim debug
                //result.AsyncWaitHandle.WaitOne();

                TestUtility.LogInformation("Client {0:D3}: Write {1} bytes: {2} ", Connection.Id, outputData.Length,
                    Encoding.UTF8.GetString(outputData, 0, outputData.Length));

                //result.AsyncWaitHandle.Close();
            }
        }

        public void SendWebSocketRequest(int websocketVersion, string AffinityCookie)
        {
            HandShakeRequest = Frames.GetHandShakeFrameWithAffinityCookie(Address.AbsoluteUri, websocketVersion, AffinityCookie);

            byte[] outputData = null;
            int offset = 0;
            while (offset < HandShakeRequest.Length)
            {
                outputData = HandShakeRequest[offset++];

                Connection.Stream.BeginWrite(outputData, 0, outputData.Length, WriteCallback, Connection);
                TestUtility.LogInformation("Client {0:D3}: Write {1} bytes: {2} ", Connection.Id, outputData.Length,
                    Encoding.UTF8.GetString(outputData, 0, outputData.Length));
            }
        }

        public void ReadDataCallback(IAsyncResult result)
        {
            WebSocketConnect client = (WebSocketConnect) result.AsyncState;
            
            if (client.IsDisposed)
                return;

            int bytesRead = client.Stream.EndRead(result); // wait until the buffer is filled 
            int bytesReadIntotal = bytesRead;
            ArrayList InputDataArray = new ArrayList();
            byte[] tempBuffer = null;

            if (bytesRead > 0)
            {
                tempBuffer = WebSocketClientUtility.SubArray(Connection.InputData, 0, bytesRead);

                Frame temp = new Frame(tempBuffer);

                // start looping if there is still remaining data
                if (tempBuffer.Length < temp.DataLength)
                { 
                    if (client.TcpClient.GetStream().DataAvailable)
                    {
                        // add the first buffer to the arrayList
                        InputDataArray.Add(tempBuffer);

                        // start looping appending to the arrayList
                        while (client.TcpClient.GetStream().DataAvailable)
                        {
                            bytesRead = client.TcpClient.GetStream().Read(Connection.InputData, 0, Connection.InputData.Length);
                            tempBuffer = WebSocketClientUtility.SubArray(Connection.InputData, 0, bytesRead);
                            InputDataArray.Add(tempBuffer);
                            bytesReadIntotal += bytesRead;
                            TestUtility.LogInformation("ReadDataCallback: Looping: Client {0:D3}: bytesReadHere {1} ", Connection.Id, bytesRead);
                        }

                        // create a single byte array with the arrayList
                        tempBuffer = new byte[bytesReadIntotal];
                        int arrayIndex = 0;
                        foreach (byte[] item in InputDataArray.ToArray())
                        {
                            for (int i = 0; i < item.Length; i++)
                            {
                                tempBuffer[arrayIndex] = item[i];
                                arrayIndex++;
                            }
                        }
                    }
                }

                // Create frame with the tempBuffer
                Frame frame = new Frame(tempBuffer);
                ProcessReceivedData(frame);
                int nextFrameIndex = frame.IndexOfNextFrame;

                while (nextFrameIndex != -1)
                {
                    tempBuffer = tempBuffer.SubArray(frame.IndexOfNextFrame, tempBuffer.Length - frame.IndexOfNextFrame);
                    frame = new Frame(tempBuffer);
                    ProcessReceivedData(frame);
                    nextFrameIndex = frame.IndexOfNextFrame;
                }

                if (client.IsDisposed)
                    return;

                // Start the Async Read to handle the next frame comming from server
                client.Stream.BeginRead(client.InputData, 0, client.InputData.Length, ReadDataCallback, client);
            }
            else
            {
                client.Dispose();
            }
        }

        public Frame ReadData()
        {
            Frame frame = new Frame(new byte[] { });
            
            IAsyncResult result = Connection.Stream.BeginRead(Connection.InputData, 0, Connection.InputData.Length, null, Connection);
            
            if (result != null)
            {
                int bytesRead = Connection.Stream.EndRead(result);
                if (bytesRead > 0)
                {
                    frame = new Frame(WebSocketClientUtility.SubArray(Connection.InputData, 0, bytesRead));

                    ProcessReceivedData(frame);

                    TestUtility.LogInformation("Client {0:D3}: Read Type {1} : {2} ", Connection.Id, frame.FrameType, frame.Content.Length);
                }

            }

            return frame;
        }

        public void SendTextData(string data)
        {
            Send(WebSocketClientUtility.GetFramedTextDataInBytes(data));
        }

        public void SendTextData(string data, byte opCode)
        {
            Send(WebSocketClientUtility.GetFramedTextDataInBytes(data, opCode));
        }

        public void SendHello()
        {
            Send(Frames.HELLO);
        }

        public void SendPing()
        {
            Send(Frames.PING);
        }

        public void SendPong()
        {
            Send(Frames.PONG);
        }
        public void SendClose()
        {
            Send(Frames.CLOSE_FRAME);
        }

        public void SendPong(Frame receivedPing)
        {
            var pong = new byte[receivedPing.Data.Length+4];
            for (int i = 1; i < receivedPing.Data.Length; i++)
            {   
                if(i<2)
                    pong[i] = receivedPing.Data[i];
                else
                    pong[i+4] = receivedPing.Data[i];
            }

            pong[0] = 0x8A;
            pong[1] = (byte)((int)pong[1] | 128);

            Send(pong);
        }

        public void CloseConnection()
        {
            this.WebSocketState = WebSocketState.ClosingFromClientStarted;
            Send(Frames.CLOSE_FRAME);

            if (!WaitForWebSocketState(WebSocketState.ConnectionClosed))
            {
                throw new Exception("Failed to close a connection");
            }
        }

        public static void WriteCallback(IAsyncResult result)
        {
            var client = result.AsyncState as WebSocketConnect;
            if (client.IsDisposed)
                return;

            client.Stream.EndWrite(result);
        }

        override public string ToString()
        {
            return Connection.Id + ": " + WebSocketState.ToString();
        }

        #region Private Methods

        public Frame Send(byte[] outputData)
        {
            var frame = new Frame(outputData);
            ProcessSentData(frame);
            if (Connection.TcpClient.Connected)
            {
                var result = Connection.Stream.BeginWrite(outputData, 0, outputData.Length, WriteCallback, Connection);
                TestUtility.LogInformation("Client {0:D3}: Write Type {1} : {2} ", Connection.Id, frame.FrameType,
                    frame.Content.Length);
            }
            else
            {
                TestUtility.LogInformation("Connection is disconnected");
            }

            return frame;
        }

        private void ProcessSentData(Frame frame)
        {
            ProcessData(frame, true);
        }

        private void ProcessReceivedData(Frame frame)
        {
            TestUtility.LogInformation("ReadDataCallback: Client {0:D3}: Read Type {1} : {2} ", Connection.Id, frame.FrameType, frame.DataLength);
            if (frame.FrameType == FrameType.NonControlFrame)
            {
                string content = frame.Content.ToLower();
                if (content.Contains("connection: upgrade")
                    && content.Contains("upgrade: websocket")
                    && content.Contains("http/1.1 101 switching protocols"))
                {
                    TestUtility.LogInformation("Connection opened...");
                    TestUtility.LogInformation(frame.Content);
                    WebSocketState = WebSocketState.ConnectionOpen;
                }
            }
            else
            {
                // Send Pong if the frame was Ping
                if (frame.FrameType == FrameType.Ping)
                    SendPong(frame);
                
                // Send Close if the frame was Close
                if (frame.FrameType == FrameType.Close)
                {
                    if (WebSocketState == WebSocketState.ConnectionClosed)
                    {
                        throw new Exception("Connection was already closed");
                    }
                    else
                    {
                        if (WebSocketState != WebSocketState.ClosingFromClientStarted)
                        {
                            TestUtility.LogInformation("Send back Close frame to responsd server closing...");
                            SendClose();
                        }
                        TestUtility.LogInformation(frame.Content);
                        WebSocketState = WebSocketState.ConnectionClosed;
                        IsOpened = false;
                    }
                }
            }
            ProcessData(frame, false);
        }

        private void ProcessData(Frame frame, bool isSentData)
        {
            if (isSentData && StoreData)
                StoreDataSent(frame);
            else if (StoreData)
                StoreDataReceived(frame);
        }

        private void StoreDataReceived(Frame frame)
        {
            Connection.DataReceived.Add(frame);
            Connection.TotalDataReceived += frame.Content.Length;
         }

        private void StoreDataSent(Frame frame)
        {
            Connection.DataSent.Add(frame);
            Connection.TotalDataSent += frame.Content.Length;
        }

        #endregion
    }
}
