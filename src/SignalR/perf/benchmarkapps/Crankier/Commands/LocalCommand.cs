// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.CommandLineUtils;

using static Microsoft.AspNetCore.SignalR.Crankier.Commands.CommandLineUtilities;

namespace Microsoft.AspNetCore.SignalR.Crankier.Commands
{
    internal sealed class LocalCommand
    {
        public static void Register(CommandLineApplication app)
        {
            app.Command("local", cmd =>
            {
                var targetUrlOption = cmd.Option("--target-url <TARGET_URL>", "The URL to run the test against.", CommandOptionType.SingleValue);
                var numberOfWorkersOption = cmd.Option("--workers <WORKER_COUNT>", "The number of workers to use.", CommandOptionType.SingleValue);
                var numberOfConnectionsOption = cmd.Option("--connections <CONNECTION_COUNT>", "The number of connections per worker to use.", CommandOptionType.SingleValue);
                var sendDurationInSecondsOption = cmd.Option("--send-duration <SEND_DURATION_IN_SECONDS>", "The send duration to use.", CommandOptionType.SingleValue);
                var transportTypeOption = cmd.Option("--transport <TRANSPORT>", "The transport to use (defaults to WebSockets).", CommandOptionType.SingleValue);
                var workerWaitForDebuggerOption = cmd.Option("--worker-debug", "Provide this switch to have the worker wait for the debugger.", CommandOptionType.NoValue);

                cmd.OnExecute(async () =>
                {
                    if (!targetUrlOption.HasValue())
                    {
                        return MissingRequiredArg(targetUrlOption);
                    }

                    var numberOfWorkers = Defaults.NumberOfWorkers;
                    var numberOfConnections = Defaults.NumberOfConnections;
                    var sendDurationInSeconds = Defaults.SendDurationInSeconds;
                    var transportType = Defaults.TransportType;

                    if (numberOfWorkersOption.HasValue() && !int.TryParse(numberOfWorkersOption.Value(), out numberOfWorkers))
                    {
                        return MissingRequiredArg(numberOfWorkersOption);
                    }

                    if (numberOfConnectionsOption.HasValue() && !int.TryParse(numberOfConnectionsOption.Value(), out numberOfConnections))
                    {
                        return InvalidArg(numberOfConnectionsOption);
                    }

                    if (sendDurationInSecondsOption.HasValue() && !int.TryParse(sendDurationInSecondsOption.Value(), out sendDurationInSeconds))
                    {
                        return InvalidArg(sendDurationInSecondsOption);
                    }

                    if (transportTypeOption.HasValue() && !Enum.TryParse(transportTypeOption.Value(), out transportType))
                    {
                        return InvalidArg(transportTypeOption);
                    }

                    return await Execute(targetUrlOption.Value(), numberOfWorkers, numberOfConnections, sendDurationInSeconds, transportType, workerWaitForDebuggerOption.HasValue());
                });
            });
        }

        private static async Task<int> Execute(string targetUrl, int numberOfWorkers, int numberOfConnections, int sendDurationInSeconds, HttpTransportType transportType, bool workerWaitForDebugger)
        {
            var agent = new Agent(workerWaitForDebugger: workerWaitForDebugger);
            var runner = new Runner(agent, targetUrl, numberOfWorkers, numberOfConnections, sendDurationInSeconds, transportType);
            try
            {
                await runner.RunAsync();
            }
            catch (Exception ex)
            {
                return Fail(ex.ToString());
            }

            return 0;
        }
    }
}
