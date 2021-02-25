// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [SkipIfHostableWebCoreNotAvailable]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
    public class ConnectionIdFeatureTests : StrictTestServerTests
    {
        [ConditionalFact]
        public async Task ProvidesConnectionId()
        {
            string connectionId = null;
            using (var testServer = await TestServer.Create(ctx => {
                var connectionIdFeature = ctx.Features.Get<IHttpConnectionFeature>();
                connectionId = connectionIdFeature.ConnectionId;
                return Task.CompletedTask;
            }, LoggerFactory))
            {
                await testServer.HttpClient.GetStringAsync("/");
            }

            Assert.NotNull(connectionId);
        }
    }
}
