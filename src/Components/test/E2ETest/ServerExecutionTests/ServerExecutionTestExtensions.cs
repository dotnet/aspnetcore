// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using TestServer;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    internal static class ServerExecutionTestExtensions
    {
        public static ToggleExecutionModeServerFixture<T> WithServerExecution<T>(this ToggleExecutionModeServerFixture<T> serverFixture)
        {
            serverFixture.UseAspNetHost(Program.BuildWebHost<ServerStartup>);
            serverFixture.ExecutionMode = ExecutionMode.Server;
            return serverFixture;
        }

        public static ToggleExecutionModeServerFixture<T> WithServerExecution<T, TStartup>(this ToggleExecutionModeServerFixture<T> serverFixture) where TStartup : class
        {
            serverFixture.UseAspNetHost(Program.BuildWebHost<TStartup>);
            serverFixture.ExecutionMode = ExecutionMode.Server;
            return serverFixture;
        }
    }
}
