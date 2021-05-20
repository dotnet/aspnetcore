// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    // The OS's being tested are on other machines, don't duplicate the tests across runs.
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    // See CrossMachineReadMe.md
    public class CrossMachineTests
    {
        private const string Http11Version = "HTTP/1.1";
        private const string Http2Version = "HTTP/2";

        private const string ClientAddress =
            // "http://chrross-udesk:5004";
            "https://localhost:5005";
        private const string ServerName =
            "chrross-dc";
            // "chrross-udesk";
        private static readonly string ServerPersistAddress = $"http://{ServerName}.CRKerberos.com:5000";
        private static readonly string ServerNonPersistAddress = $"http://{ServerName}.CRKerberos.com:5002";

        public static IEnumerable<object[]> Http11And2 =>
            new List<object[]>
            {
                new object[] { Http11Version },
                new object[] { Http2Version },
            };

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(Http11And2))]
        public Task Anonymous_NoChallenge_NoOps(string protocol)
        {
            return RunTest(protocol, "/Anonymous/Unrestricted");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(Http11And2))]
        public Task Anonymous_Challenge_401Negotiate(string protocol)
        {
            return RunTest(protocol, "/Anonymous/Authorized");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(Http11And2))]
        public Task DefautCredentials_Success(string protocol)
        {
            return RunTest(protocol, "/DefaultCredentials/Authorized");
        }

        public static IEnumerable<object[]> HttpOrders =>
            new List<object[]>
            {
                new object[] { Http11Version, Http11Version },
                new object[] { Http11Version, Http2Version },
                new object[] { Http2Version, Http11Version },
            };

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(HttpOrders))]
        // AuthorizedRequestAfterAuth_ReUses1WithPersistence would give the same results
        public Task UrestrictedRequestAfterAuth_ReUses1WithPersistence(string protocol1, string protocol2)
        {
            return RunTest(ServerPersistAddress, protocol1, protocol2, "/AfterAuth/Unrestricted/Persist");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(HttpOrders))]
        public Task UrestrictedRequestAfterAuth_AnonymousWhenNotPersisted(string protocol1, string protocol2)
        {
            return RunTest(ServerNonPersistAddress, protocol1, protocol2, "/AfterAuth/Unrestricted/NonPersist");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(HttpOrders))]
        public Task AuthorizedRequestAfterAuth_ReauthenticatesWhenNotPersisted(string protocol1, string protocol2)
        {
            return RunTest(ServerNonPersistAddress, protocol1, protocol2, "/AfterAuth/Authorized/NonPersist");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(Http11And2))]
        public Task Unauthorized_401Negotiate(string protocol)
        {
            return RunTest(protocol, "/Unauthorized");
        }

        [ConditionalTheory(Skip = "Manual testing only")]
        [MemberData(nameof(Http11And2))]
        public Task UnauthorizedAfterAuthenticated_Success(string protocol)
        {
            return RunTest(protocol, "/AfterAuth/Unauthorized", persist: true);
        }

        private Task RunTest(string protocol, string path, bool persist = false)
        {
            var queryBuilder = new QueryBuilder
            {
                { "server", persist ? ServerPersistAddress : ServerNonPersistAddress },
                { "protocol", protocol }
            };

            return RunTest(path, queryBuilder);
        }

        private Task RunTest(string server, string protocol1, string protocol2, string path)
        {
            var queryBuilder = new QueryBuilder
            {
                { "server", server },
                { "protocol1", protocol1 },
                { "protocol2", protocol2 }
            };

            return RunTest(path, queryBuilder);
        }

        private async Task RunTest(string path, QueryBuilder queryBuilder)
        {
            using var client = CreateClient(ClientAddress);

            var response = await client.GetAsync("/authtest" + path + queryBuilder.ToString());
            var body = await response.Content.ReadAsStringAsync();

            Assert.True(HttpStatusCode.OK == response.StatusCode, $"{response.StatusCode}: {body}");
        }

        private static HttpClient CreateClient(string address)
        {
            return new HttpClient(new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            })
            {
                BaseAddress = new Uri(address),
                DefaultRequestVersion = new Version(2, 0),
            };
        }
    }
}
