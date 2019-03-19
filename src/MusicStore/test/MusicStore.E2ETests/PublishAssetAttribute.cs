// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace MusicStore
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class PublishAssetAttribute : Attribute
    {
        public PublishAssetAttribute(string architecture, string applicationType, string server, string hostingModel, string publishDirectory)
        {
            RuntimeArchitecture = Enum.Parse<RuntimeArchitecture>(architecture);
            ApplicationType = Enum.Parse<ApplicationType>(applicationType);
            ServerType = Enum.Parse<ServerType>(server);
            HostingModel = Enum.Parse<HostingModel>(hostingModel);
            PublishDirectory = publishDirectory;
        }

        internal TestVariant GetTestVariant()
        {
            return new TestVariant
            {
                ApplicationType = ApplicationType,
                Architecture = RuntimeArchitecture,
                HostingModel = HostingModel,
                Server = ServerType,
                Tfm = Tfm.NetCoreApp30,
            };
        }

        public RuntimeArchitecture RuntimeArchitecture { get; }

        public ApplicationType ApplicationType { get; }

        public ServerType ServerType { get; }

        public HostingModel HostingModel { get; }

        public string PublishDirectory { get; }
    }

}
