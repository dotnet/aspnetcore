// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal class ServerCommand : CommandBase
    {
        public ServerCommand(Application parent)
            : base(parent, "server")
        {
            Pipe = Option("-p|--pipe", "name of named pipe", CommandOptionType.SingleValue);
        }

        public CommandOption Pipe { get; }

        protected override bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(Pipe.Value()))
            {
                Pipe.Values.Add(PipeName.ComputeDefault());
            }

            return true;
        }

        protected override Task<int> ExecuteCoreAsync()
        {
            // Make sure there's only one server with the same identity at a time.
            using (var mutex = new Mutex(initiallyOwned: true, name: MutexName.GetServerMutexName(Pipe.Value()), createdNew: out var holdsMutex))
            {
                if (!holdsMutex)
                {
                    // Another server is running, just exit.
                    Error.Write("Another server already running...");
                    return Task.FromResult(1);
                }

                try
                {
                    var host = ConnectionHost.Create(Pipe.Value());
                    var compilerHost = CompilerHost.Create();
                    var dispatcher = RequestDispatcher.Create(host, compilerHost, Cancelled);
                    dispatcher.Run();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            return Task.FromResult(0);
        }
    }
}
