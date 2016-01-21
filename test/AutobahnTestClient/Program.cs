// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets.Client;

namespace AutobahnTestClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Run(args).Wait();
        }
        
        private async Task Run(string[] args)
        {
            try
            {
                string serverAddress = "ws://localhost:5000";
                string agent =
                    "ManagedWebSockets";
                    // "NativeWebSockets";

                Console.WriteLine("Getting case count.");
                var caseCount = await GetCaseCountAsync(serverAddress, agent);
                Console.WriteLine(caseCount + " case(s).");

                for (int i = 1; i <= caseCount; i++)
                {
                    await RunCaseAsync(serverAddress, i, agent);
                }

                await UpdateReportsAsync(serverAddress, agent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private async Task<WebSocket> ConnectAsync(string address, string agent)
        {
            if (string.Equals(agent, "NativeWebSockets"))
            {
                var client = new ClientWebSocket();
                await client.ConnectAsync(new Uri(address), CancellationToken.None);
                return client;
            }
            else
            {
                // TODO: BUG: Require ws or wss schemes
                var client = new WebSocketClient();
                return await client.ConnectAsync(new Uri(address), CancellationToken.None);
            }
        }

        private async Task<int> GetCaseCountAsync(string serverAddress, string agent)
        {
            var webSocket = await ConnectAsync(serverAddress + "/getCaseCount", agent);
            byte[] buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            var caseCountText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return int.Parse(caseCountText);
        }

        private async Task RunCaseAsync(string serverAddress, int caseId, string agent)
        {
            try
            {
                Console.WriteLine("Running case " + caseId);
                var webSocket = await ConnectAsync(serverAddress + "/runCase?case=" + caseId + "&agent=" + agent, agent);
                await Echo(webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            byte[] buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private async Task UpdateReportsAsync(string serverAddress, string agent)
        {
            var webSocket = await ConnectAsync(serverAddress + "/updateReports?agent=" + agent, agent);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }
}
