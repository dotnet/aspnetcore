// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MsgPack.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class SignalRDependencyInjectionExtensionsTests
    {
        [Fact]
        public void JSonSerializerSettingsShouldNotBeNullInOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSignalR();
            var serviceProvider = services.BuildServiceProvider();
            var hubOptions = serviceProvider.GetService<IOptions<HubOptions>>();
            Assert.NotNull(hubOptions.Value.JsonSerializerSettings);
        }

        [Fact]
        public void MessagePackSerializationContextInOptionsIsSetAndHasDefaultSettings()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSignalR();
            var serviceProvider = services.BuildServiceProvider();
            var hubOptions = serviceProvider.GetService<IOptions<HubOptions>>();
            var serializationContext = hubOptions.Value.MessagePackSerializationContext;
            Assert.NotNull(serializationContext);
            Assert.Equal(SerializationMethod.Map, serializationContext.SerializationMethod);
            Assert.True(serializationContext.CompatibilityOptions.AllowAsymmetricSerializer);
        }
    }
}
