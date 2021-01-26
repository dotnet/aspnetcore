// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class TestVariant : IXunitSerializable
    {
        public ServerType Server { get; set; }
        public string Tfm { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public RuntimeArchitecture Architecture { get; set; }

        public string Skip { get; set; }

        // ANCM specifics...
        public HostingModel HostingModel { get; set; }

        public override string ToString()
        {
            // For debug and test explorer view
            var description = $"Server: {Server}, TFM: {Tfm}, Type: {ApplicationType}, Arch: {Architecture}";
            if (Server == ServerType.IISExpress || Server == ServerType.IIS)
            {
                description += $", Host: {HostingModel}";
            }
            return description;
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Skip), Skip, typeof(string));
            info.AddValue(nameof(Server), Server, typeof(ServerType));
            info.AddValue(nameof(Tfm), Tfm, typeof(string));
            info.AddValue(nameof(ApplicationType), ApplicationType, typeof(ApplicationType));
            info.AddValue(nameof(Architecture), Architecture, typeof(RuntimeArchitecture));
            info.AddValue(nameof(HostingModel), HostingModel, typeof(HostingModel));
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Skip = info.GetValue<string>(nameof(Skip));
            Server = info.GetValue<ServerType>(nameof(Server));
            Tfm = info.GetValue<string>(nameof(Tfm));
            ApplicationType = info.GetValue<ApplicationType>(nameof(ApplicationType));
            Architecture = info.GetValue<RuntimeArchitecture>(nameof(Architecture));
            HostingModel = info.GetValue<HostingModel>(nameof(HostingModel));
        }
    }
}
