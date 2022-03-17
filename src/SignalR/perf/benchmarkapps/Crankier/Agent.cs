// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Agent : IAgent
    {
        private readonly bool _workerWaitForDebugger;
        private readonly string _hostName;

        private readonly ConcurrentDictionary<int, AgentWorker> _workers;
        private readonly string _executable;

        public Agent(string executable = null, bool workerWaitForDebugger = false)
        {
            _workerWaitForDebugger = workerWaitForDebugger;
            _executable = executable ?? GetMyExecutable();

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            _hostName = Dns.GetHostName();

            _workers = new ConcurrentDictionary<int, AgentWorker>();

            Trace.WriteLine("Agent created");
        }

        private string GetMyExecutable()
        {
            var mainModuleFile = Process.GetCurrentProcess().MainModule.FileName;
            if (Path.GetFileNameWithoutExtension(mainModuleFile).Equals("dotnet"))
            {
                // We're running in 'dotnet'
                return Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.dll");
            }
            else
            {
                // Standalone deployment
                return mainModuleFile;
            }
        }

        public IRunner Runner { get; set; }

        public string TargetAddress { get; private set; }

        public int TotalConnectionsRequested { get; private set; }

        public bool ApplyingLoad { get; private set; }

        public AgentHeartbeatInformation GetHeartbeatInformation()
        {
            return new AgentHeartbeatInformation
            {
                HostName = _hostName,
                TargetAddress = TargetAddress,
                TotalConnectionsRequested = TotalConnectionsRequested,
                ApplyingLoad = ApplyingLoad,
                Workers = _workers.Select(worker => worker.Value.GetHeartbeatInformation()).ToList()
            };
        }

        public Dictionary<int, StatusInformation> GetWorkerStatus()
        {
            return _workers.Values.ToDictionary(
                k => k.Id,
                v => v.StatusInformation);
        }

        private AgentWorker CreateWorker()
        {
            var fileName = _executable;
            var arguments = $"worker --agent {Environment.ProcessId}";
            if (_workerWaitForDebugger)
            {
                arguments += " --wait-for-debugger";
            }
            if (fileName.EndsWith(".dll"))
            {
                // Execute using dotnet.exe
                fileName = GetDotNetHost();
                arguments = _executable + " " + arguments;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var worker = new AgentWorker(startInfo, this);

            worker.StatusInformation = new StatusInformation();

            worker.Start();

            worker.OnError += OnError;
            worker.OnExit += OnExit;

            _workers.TryAdd(worker.Id, worker);

            return worker;
        }

        private static string GetDotNetHost() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        private async Task StartWorker(int id, string targetAddress, HttpTransportType transportType, int numberOfConnectionsPerWorker)
        {
            if (_workers.TryGetValue(id, out var worker))
            {
                await worker.Worker.ConnectAsync(targetAddress, transportType, numberOfConnectionsPerWorker);
            }
        }

        public async Task StartWorkersAsync(string targetAddress, int numberOfWorkers, HttpTransportType transportType, int numberOfConnections)
        {
            TargetAddress = targetAddress;
            TotalConnectionsRequested = numberOfConnections;

            var connectionsPerWorker = numberOfConnections / numberOfWorkers;
            var remainingConnections = numberOfConnections % numberOfWorkers;

            async Task RunWorker(int index, AgentWorker worker)
            {
                if (index == 0)
                {
                    await StartWorker(worker.Id, targetAddress, transportType, connectionsPerWorker + remainingConnections);
                }
                else
                {
                    await StartWorker(worker.Id, targetAddress, transportType, connectionsPerWorker);
                }

                await Runner.LogAgentAsync("Agent started listening to worker {0} ({1} of {2}).", worker.Id, index + 1, numberOfWorkers);
            }

            var workerTasks = new Task<AgentWorker>[numberOfWorkers];
            for (var index = 0; index < numberOfWorkers; index++)
            {
                workerTasks[index] = Task.Run(() => CreateWorker());
            }

            await Task.WhenAll(workerTasks);

            for (var index = 0; index < numberOfWorkers; index++)
            {
                _ = RunWorker(index, workerTasks[index].Result);
            }
        }

        public void KillWorker(int workerId)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Kill();
                Runner.LogAgentAsync("Agent killed Worker {0}.", workerId);
            }
        }

        public void KillWorkers(int numberOfWorkersToKill)
        {
            var keys = _workers.Keys.Take(numberOfWorkersToKill).ToList();

            foreach (var key in keys)
            {
                if (_workers.TryGetValue(key, out var worker))
                {
                    worker.Kill();
                    Runner.LogAgentAsync("Agent killed Worker {0}.", key);
                }
            }
        }

        public void KillConnections()
        {
            var keys = _workers.Keys.ToList();

            foreach (var key in keys)
            {
                if (_workers.TryGetValue(key, out var worker))
                {
                    worker.Kill();
                    Runner.LogAgentAsync("Agent killed Worker {0}.", key);
                }
            }

            TotalConnectionsRequested = 0;
            ApplyingLoad = false;
        }

        public void PingWorker(int workerId, int value)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Worker.PingAsync(value);
                Runner.LogAgentAsync("Agent sent ping command to Worker {0} with value {1}.", workerId, value);
            }
            else
            {
                Runner.LogAgentAsync("Agent failed to send ping command, Worker {0} not found.", workerId);
            }
        }

        public void StartTest(int messageSize, TimeSpan sendInterval)
        {
            ApplyingLoad = true;

            Task.Run(() =>
            {
                foreach (var worker in _workers.Values)
                {
                    worker.Worker.StartTestAsync(sendInterval, messageSize);
                }
            });
        }

        public void StopWorker(int workerId)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Worker.StopAsync();
            }
        }

        public async Task StopWorkersAsync()
        {
            var keys = _workers.Keys.ToList();

            foreach (var key in keys)
            {
                if (_workers.TryGetValue(key, out var worker))
                {
                    await worker.Worker.StopAsync();
                    await Runner.LogAgentAsync("Agent stopped Worker {0}.", key);
                }
            }
            TotalConnectionsRequested = 0;
            ApplyingLoad = false;

            // Wait for workers to terminate
            while (_workers.Count > 0)
            {
                await Task.Delay(1000);
            }
        }

        public async Task PongAsync(int id, int value)
        {
            await Runner.LogAgentAsync("Agent received pong message from Worker {0} with value {1}.", id, value);
            await Runner.PongWorkerAsync(id, value);
        }

        public async Task LogAsync(int id, string text)
        {
            await Runner.LogWorkerAsync(id, text);
        }

        public Task StatusAsync(
            int id,
            StatusInformation statusInformation)
        {
            if (_workers.TryGetValue(id, out var worker))
            {
                worker.StatusInformation = statusInformation;
            }

            return Task.CompletedTask;
        }

        private void OnError(int workerId, Exception ex)
        {
            Runner.LogWorkerAsync(workerId, ex.Message);
        }

        private void OnExit(int workerId, int exitCode)
        {
            _workers.TryRemove(workerId, out _);
            var message = $"Worker {workerId} exited with exit code {exitCode}.";
            Trace.WriteLine(message);
            if (exitCode != 0)
            {
                throw new Exception(message);
            }
        }
    }
}
