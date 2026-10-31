// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class AgentSender : IAgent
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly StreamWriter _outputStreamWriter;

        public AgentSender(StreamWriter outputStreamWriter)
        {
            _outputStreamWriter = outputStreamWriter;
        }

        public async Task PongAsync(int id, int value)
        {
            var parameters = new
            {
                Id = id,
                Value = value
            };

            await SendAsync("pong", JToken.FromObject(parameters));
        }

        public async Task LogAsync(int id, string text)
        {
            var parameters = new
            {
                Id = id,
                Text = text
            };

            await SendAsync("log", JToken.FromObject(parameters));
        }

        public async Task StatusAsync(
            int id,
            StatusInformation statusInformation)
        {
            var parameters = new
            {
                Id = id,
                StatusInformation = statusInformation
            };

            await SendAsync("status", JToken.FromObject(parameters));
        }

        private async Task SendAsync(string method, JToken parameters)
        {
            await _lock.WaitAsync();
            try
            {
                await _outputStreamWriter.WriteLineAsync(
                    JsonConvert.SerializeObject(new Message
                    {
                        Command = method,
                        Value = parameters
                    }));
                await _outputStreamWriter.FlushAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
