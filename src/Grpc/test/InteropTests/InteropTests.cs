// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InteropTests.Helpers;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace InteropTests
{
    public class InteropTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        private readonly string _clientPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropClient", "InteropClient.dll");
        private readonly string _serverPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropWebsite", "InteropWebsite.dll");
        private readonly ITestOutputHelper _output;

        public InteropTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/22101")]
        [MemberData(nameof(TestCaseData))]
        public async Task InteropTestCase(string name)
        {
            using (var serverProcess = new WebsiteProcess(_serverPath, _output))
            {
                await serverProcess.WaitForReady().TimeoutAfter(DefaultTimeout);

                using (var clientProcess = new ClientProcess(_output, _clientPath, serverProcess.ServerPort, name))
                {
                    await clientProcess.WaitForReady().TimeoutAfter(DefaultTimeout);

                    await clientProcess.Exited.TimeoutAfter(DefaultTimeout);

                    Assert.Equal(0, clientProcess.ExitCode);
                }
            }
        }

        #region TestData
        // All interop test cases, minus GCE authentication specific tests
        private static string[] AllTests = new string[]
        {
            "empty_unary",
            "large_unary",
            "client_streaming",
            "server_streaming",
            "ping_pong",
            "empty_stream",

            "cancel_after_begin",
            "cancel_after_first_response",
            "timeout_on_sleeping_server",
            "custom_metadata",
            "status_code_and_message",
            "special_status_message",
            "unimplemented_service",
            "unimplemented_method",
            "client_compressed_unary",
            "client_compressed_streaming",
            "server_compressed_unary",
            "server_compressed_streaming"
        };

        public static IEnumerable<object[]> TestCaseData => AllTests.Select(t => new object[] { t });
        #endregion
    }
}
