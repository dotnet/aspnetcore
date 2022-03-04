// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    internal static class ServerExecutionTestExtensions
    {
        public static ToggleExecutionModeServerFixture<T> WithServerExecution<T>(this ToggleExecutionModeServerFixture<T> serverFixture)
        {
            serverFixture.UseAspNetHost(TestServer.Program.BuildWebHost);
            serverFixture.ExecutionMode = ExecutionMode.Server;
            return serverFixture;
        }
    }
}
